using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.FileProviders;
using MyAspNetApp.Data;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
// Register profile-related services
builder.Services.AddScoped<MyAspNetApp.Services.OrderService>();

// Add database context
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");

var connectionStringBuilder = new SqlConnectionStringBuilder(rawConnectionString)
{
    Encrypt = true,
    TrustServerCertificate = true,
    Pooling = true
};

if (connectionStringBuilder.ConnectTimeout < 30)
{
    connectionStringBuilder.ConnectTimeout = 30;
}

if (connectionStringBuilder.MaxPoolSize < 200)
{
    connectionStringBuilder.MaxPoolSize = 200;
}

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

// Configure the HTTP request pipeline.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;
        var isDbTimeout =
            exception is TimeoutException ||
            exception is SqlException ||
            (exception is InvalidOperationException ioe && ioe.Message.Contains("connection from the pool", StringComparison.OrdinalIgnoreCase)) ||
            exception?.InnerException is TimeoutException ||
            exception?.InnerException is SqlException ||
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

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

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
app.UseAuthorization();

// MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
