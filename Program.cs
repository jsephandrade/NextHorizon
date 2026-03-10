using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;

// When the app runs as a compiled exe from bin\Debug\net10.0, the working directory
// is the bin folder and wwwroot cannot be found. Walk up parent directories until
// we find the folder that contains wwwroot, and use that as the project root.
static string FindProjectRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory); 
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "wwwroot")))
            return dir.FullName;
        dir = dir.Parent;
    }
    return Directory.GetCurrentDirectory();
}

var projectRoot = FindProjectRoot();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = projectRoot,
    WebRootPath = Path.Combine(projectRoot, "wwwroot")
});

// Add MVC services
builder.Services.AddControllersWithViews();

// Add database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name     = ".NextHorizon.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout     = TimeSpan.FromHours(8);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseSession();
app.UseAuthorization();

// MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
