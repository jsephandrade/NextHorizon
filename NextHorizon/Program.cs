using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// ✅ Force HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    // Use 308 permanent redirect (recommended for browsers)
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;

    // Specify the HTTPS port you want (7172)
    options.HttpsPort = 7172;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Enforce HTTPS in production
}

// ✅ This ensures HTTP → HTTPS redirection
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=SellerDashboard}/{id?}");

app.Run();