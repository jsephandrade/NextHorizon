using FluentValidation;
using NextHorizon.Data;
using NextHorizon.Data.Messaging;
using NextHorizon.Models.Agent;
using NextHorizon.Models;
using NextHorizon.Modules.MemberTracker.Data;
using NextHorizon.Modules.MemberTracker.Models;
using NextHorizon.Modules.MemberTracker.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Security;
using NextHorizon.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(defaultConnection));
builder.Services.AddScoped<ICustomerStoredProcedureService, CustomerStoredProcedureService>();
builder.Services.AddScoped<IMemberUploadRepository, MemberUploadStoredProcedureRepository>();
builder.Services.AddScoped<IMessagingRepository, MessagingStoredProcedureRepository>();
builder.Services.AddScoped<IOrderConversationResolver, SimulatedOrderConversationResolver>();
builder.Services.AddScoped<IAuthenticatedUserContextService, AuthenticatedUserContextService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAgentDashboardService, AgentDashboardService>();
builder.Services.AddScoped<IAgentRankingService, AgentRankingService>();
builder.Services.AddScoped<IQaAgentTicketsService, QaAgentTicketsService>();
builder.Services.AddScoped<IQaAgentsService, QaAgentsService>();
builder.Services.AddScoped<IQaDashboardService, QaDashboardService>();
builder.Services.AddScoped<IQaRatingQueueService, QaRatingQueueService>();
builder.Services.AddScoped<IQaRatedHistoryService, QaRatedHistoryService>();
builder.Services.AddScoped<IQaResolvedTicketsService, QaResolvedTicketsService>();
builder.Services.AddScoped<IQaReviewService, QaReviewService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddTransient<IValidator<CreateMemberUploadRequest>, CreateMemberUploadRequestValidator>();
builder.Services.AddTransient<IValidator<UpdateMemberUploadRequest>, UpdateMemberUploadRequestValidator>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(DevelopmentAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        UploadAuthorizationPolicies.ConsumerUpload,
        policy => policy.RequireRole(UploadRoles.Consumer, UploadRoles.Admin));
    options.AddPolicy(
        UploadAuthorizationPolicies.ViewAllUploads,
        policy => policy.RequireRole(UploadRoles.Admin, UploadRoles.Moderator));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("upload-write", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("csrf-token", limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("conversation-create", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("message-send", limiter =>
    {
        limiter.PermitLimit = 45;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("message-read", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("help-search", limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("help-ticket-create", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(5);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var rankingService = scope.ServiceProvider.GetRequiredService<IAgentRankingService>();
    await rankingService.RecomputeAllQaMonthsAsync(CancellationToken.None);
    await rankingService.RecomputeMonthAsync(AgentRankingMetricType.AverageHandlingTime, DateTime.UtcNow, CancellationToken.None);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

