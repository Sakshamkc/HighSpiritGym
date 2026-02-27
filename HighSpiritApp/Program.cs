using HighSpiritApp.DataContext;
using HighSpiritApp.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// =============================================================
// COOKIE/SESSION TIMEOUT CONFIGURATION
// =============================================================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";

    // Session timeout settings - Set to 15 minutes for testing (change to 15 for production)
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15); // Session expires after 15 minutes
    options.SlidingExpiration = true; // Reset timer on each request (activity extends session)

    // Cookie settings
    options.Cookie.Name = "HighSpiritAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// =============================================================
// REPOSITORY & SERVICE REGISTRATION (Layered Architecture)
// =============================================================
builder.Services.AddRepositories();      // Data Access Layer
builder.Services.AddApplicationServices(); // Business Logic Layer

// =============================================================
// JWT AUTHENTICATION (for Mobile API)
// =============================================================
var jwtKey = builder.Configuration["Jwt:Key"] ?? "HighSpiritGym_SuperSecret_JWT_Key_2026_!@#$%^";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HighSpiritApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HighSpiritMobileApp";

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// =============================================================
// CORS (for mobile app access)
// =============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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

app.UseCors("MobileApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map API controllers (attribute-routed)
app.MapControllers();

// =============================================================
// SEED ROLES
// =============================================================
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Customer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Ensure existing admin user has Admin role
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var adminUser = await userManager.FindByNameAsync("admin");
    if (adminUser != null)
    {
        var userRoles = await userManager.GetRolesAsync(adminUser);
        if (!userRoles.Contains("Admin"))
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.Run();
