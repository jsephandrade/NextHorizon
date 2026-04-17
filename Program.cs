using NextHorizon.Data;
using NextHorizon.Models.Agent;
using NextHorizon.Models;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Services;

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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAgentDashboardService, AgentDashboardService>();
builder.Services.AddScoped<IAgentRankingService, AgentRankingService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

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

app.UseSession();

app.MapStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=AdminLogin}/{id?}")
    .WithStaticAssets();


app.Run();
