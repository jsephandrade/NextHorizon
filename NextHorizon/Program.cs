using FluentValidation;
using NextHorizon.Data;
using NextHorizon.Data.Messaging;
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
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(defaultConnection));
builder.Services.AddScoped<ICustomerStoredProcedureService, CustomerStoredProcedureService>();
builder.Services.AddScoped<IMemberUploadRepository, MemberUploadStoredProcedureRepository>();
builder.Services.AddScoped<IMessagingRepository, MessagingStoredProcedureRepository>();
builder.Services.AddScoped<IOrderConversationResolver, SimulatedOrderConversationResolver>();
builder.Services.AddScoped<IAuthenticatedUserContextService, AuthenticatedUserContextService>();
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
});

var app = builder.Build();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

