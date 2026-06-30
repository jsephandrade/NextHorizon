using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MyAspNetApp.Data;
using MyAspNetApp.Data.Messaging;
using MyAspNetApp.Models;
using MyAspNetApp.Security;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, ".aspnet", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

// Add MVC services
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".NextHorizon.Products.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
// Register profile-related services
builder.Services.AddScoped<MyAspNetApp.Services.OrderService>();
builder.Services.AddScoped<MyAspNetApp.Services.LeaderboardService>();
builder.Services.AddScoped<MyAspNetApp.Services.MediaPathService>();
builder.Services.AddScoped<MyAspNetApp.Services.ChallengeNotificationService>();
builder.Services.AddScoped<IMessagingRepository, MessagingStoredProcedureRepository>();
builder.Services.AddScoped<IAuthenticatedUserContextService, AuthenticatedUserContextService>();

// Add database context
var rawConnectionString = DbConnectionStringResolver.ResolveRequiredConnectionString(
    builder.Configuration,
    builder.Environment);

var connectionStringBuilder = new SqlConnectionStringBuilder(rawConnectionString)
{
    Encrypt = true,
    TrustServerCertificate = true,
    Pooling = true
};

if (string.IsNullOrWhiteSpace(connectionStringBuilder.ConnectionString))
{
    throw new InvalidOperationException("Resolved DefaultConnection is empty.");
}

builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionStringBuilder.ConnectionString;

if (connectionStringBuilder.ConnectTimeout < 30)
{
    connectionStringBuilder.ConnectTimeout = 30;
}

if (connectionStringBuilder.MaxPoolSize < 200)
{
    connectionStringBuilder.MaxPoolSize = 200;
}

var startupInitializationConnectionStringBuilder = new SqlConnectionStringBuilder(connectionStringBuilder.ConnectionString)
{
    Pooling = false,
    MinPoolSize = 0
};

startupInitializationConnectionStringBuilder.ConnectTimeout =
    Math.Min(connectionStringBuilder.ConnectTimeout, 5);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionStringBuilder.ConnectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 2,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: new[] { -2, 4060, 40197, 40501, 40613, 49918, 49919, 49920 });
            sqlOptions.CommandTimeout(60);
        }));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            await EnsureDatabaseSchemasAfterStartupAsync(
                app.Logger,
                startupInitializationConnectionStringBuilder.ConnectionString);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Database schema initialization failed after application startup. The app will continue to run, but database-backed features may be unavailable.");
        }
    });
});

// Configure the HTTP request pipeline.
// For debugging, always show the developer exception page so we can see the real error.
app.UseDeveloperExceptionPage();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;
        if (exception != null)
        {
            app.Logger.LogError(exception, "Unhandled exception caught by exception handler.");
        }

        var isDbTimeout =
            exception is TimeoutException ||
            exception is SqlException ||
            (exception is InvalidOperationException invalidOperationException &&
             invalidOperationException.Message.Contains("connection string", StringComparison.OrdinalIgnoreCase)) ||
            (exception is InvalidOperationException ioe && ioe.Message.Contains("connection from the pool", StringComparison.OrdinalIgnoreCase)) ||
            exception?.InnerException is TimeoutException ||
            exception?.InnerException is SqlException ||
            (exception?.InnerException is InvalidOperationException innerInvalidOperationException &&
             innerInvalidOperationException.Message.Contains("connection string", StringComparison.OrdinalIgnoreCase)) ||
            (exception?.InnerException is InvalidOperationException innerIoe && innerIoe.Message.Contains("connection from the pool", StringComparison.OrdinalIgnoreCase));

        if (isDbTimeout)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    message = "Database is temporarily unavailable. Please try again."
                }));
                return;
            }

            context.Response.Redirect("/Home/Error");
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Unexpected server error."
            }));
        }
        else
        {
            context.Response.Redirect("/Home/Error");
        }
    });
});

app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();
// Serve files from the etc-css folder at /etc-css (if present)
var etcCssPath = Path.Combine(builder.Environment.ContentRootPath, "etc-css");
if (Directory.Exists(etcCssPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(etcCssPath),
        RequestPath = "/etc-css"
    });
}
app.UseRouting();
app.UseCors();
app.UseSession();
app.Use(async (context, next) =>
{
    const string sharedUserIdCookie = "NextHorizon.SharedUserId";
    const string sharedUserEmailCookie = "NextHorizon.SharedUserEmail";
    const string sharedUserTypeCookie = "NextHorizon.SharedUserType";
    const string sharedDisplayNameCookie = "NextHorizon.SharedDisplayName";
    const string sharedCartCookie = "NextHorizon.SharedCart";

    if (!context.Session.GetInt32("UserId").HasValue &&
        int.TryParse(context.Request.Cookies[sharedUserIdCookie], out var userId))
    {
        context.Session.SetInt32("UserId", userId);
    }

    var sharedEmail = context.Request.Cookies[sharedUserEmailCookie];
    if (!string.IsNullOrWhiteSpace(sharedEmail) && string.IsNullOrWhiteSpace(context.Session.GetString("UserEmail")))
    {
        context.Session.SetString("UserEmail", sharedEmail);
    }

    var sharedUserType = context.Request.Cookies[sharedUserTypeCookie];
    if (!string.IsNullOrWhiteSpace(sharedUserType) && string.IsNullOrWhiteSpace(context.Session.GetString("UserType")))
    {
        context.Session.SetString("UserType", sharedUserType);
    }

    var sharedDisplayName = context.Request.Cookies[sharedDisplayNameCookie];
    if (!string.IsNullOrWhiteSpace(sharedDisplayName) && string.IsNullOrWhiteSpace(context.Session.GetString("DisplayName")))
    {
        context.Session.SetString("DisplayName", sharedDisplayName);
    }

    var sharedCart = context.Request.Cookies[sharedCartCookie];
    if (!string.IsNullOrWhiteSpace(sharedCart))
    {
        try
        {
            var cartItems = JsonSerializer.Deserialize<List<CartItem>>(sharedCart) ?? new List<CartItem>();
            ProductData.ReplaceCart(cartItems);
        }
        catch (JsonException)
        {
            ProductData.ReplaceCart(Array.Empty<CartItem>());
        }
    }
    else
    {
        ProductData.ReplaceCart(Array.Empty<CartItem>());
    }

    await next();
});
app.UseAuthorization();

// MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task EnsureDatabaseSchemasAfterStartupAsync(
    ILogger logger,
    string startupConnectionString,
    CancellationToken cancellationToken = default)
{
    using var startupInitializationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    startupInitializationCts.CancelAfter(TimeSpan.FromSeconds(20));

    static DbContextOptions<AppDbContext> CreateStartupDbOptions(string connectionString)
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 1,
                        maxRetryDelay: TimeSpan.FromSeconds(2),
                        errorNumbersToAdd: new[] { -2, 4060, 40197, 40501, 40613, 49918, 49919, 49920 });
                    sqlOptions.CommandTimeout(15);
                })
            .Options;

    try
    {
        await using var connectivityDbContext = new AppDbContext(CreateStartupDbOptions(startupConnectionString));
        var canConnect = await connectivityDbContext.Database.CanConnectAsync(startupInitializationCts.Token);
        if (!canConnect)
        {
            logger.LogWarning("Skipping database schema initialization after startup because the SQL Server connection check failed.");
            return;
        }

        await using (var messagingDbContext = new AppDbContext(CreateStartupDbOptions(startupConnectionString)))
        {
            await DatabaseMessagingInitializer.EnsureSchemaAsync(messagingDbContext, startupInitializationCts.Token);
        }

        await using (var commerceDbContext = new AppDbContext(CreateStartupDbOptions(startupConnectionString)))
        {
            await DatabaseSchemaInitializer.EnsureCommerceSchemaAsync(commerceDbContext, logger, startupInitializationCts.Token);
        }
    }
    catch (OperationCanceledException) when (startupInitializationCts.IsCancellationRequested)
    {
        logger.LogWarning("Skipping database schema initialization after startup because the SQL Server connection check timed out.");
    }
    catch (SqlException ex) when (ex.Number is -2 or 53 or 258)
    {
        logger.LogWarning(ex, "Skipping database schema initialization after startup because the SQL Server instance is unreachable.");
    }
    catch (SqlException ex) when (ex.Number == 3980)
    {
        logger.LogWarning(ex, "Skipping database schema initialization after startup because the SQL session was cancelled while initialization was running.");
    }
}
