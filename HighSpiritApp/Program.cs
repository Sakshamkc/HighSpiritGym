using HighSpiritApp.DataContext;
using HighSpiritApp.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =============================================================
// DATABASE CONFIGURATION
// =============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<GymDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// =============================================================
// IDENTITY CONFIGURATION
// =============================================================
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings (adjust as needed)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// =============================================================
// REPOSITORY & SERVICE REGISTRATION (Layered Architecture)
// =============================================================
builder.Services.AddRepositories();      // Data Access Layer
builder.Services.AddApplicationServices(); // Business Logic Layer

// =============================================================
// MVC CONFIGURATION
// =============================================================
builder.Services.AddControllersWithViews();

var app = builder.Build();

// =============================================================
// MIDDLEWARE PIPELINE
// =============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
