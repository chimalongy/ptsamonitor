using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using ptsamonitor.Classes.Utils;
using ptsamonitor.Data;
using ptsamonitor.Services;

var builder = WebApplication.CreateBuilder(args);

string connstring = "User Id=TADEYI;Password=tadeyi123;Data Source=localhost:1521/xepdb1;Pooling=true;Min Pool Size=1;Max Pool Size=50;";
string encryptedConnstring = Cryptor.Encrypt(connstring, true);


// ── Database ──────────────────────────────────────────────────────────────────
string? encryptedConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(encryptedConnectionString))
    throw new Exception("Connection string 'DefaultConnection' not found.");

string decryptedConnectionString = Cryptor.Decrypt(encryptedConnectionString, true);

// Validate Oracle connection string format
if (!decryptedConnectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Invalid Oracle connection string. Expected 'Data Source' parameter.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(decryptedConnectionString, opts =>
        opts.MigrationsHistoryTable("__EFMigrationsHistory", "TADEYI")));

// ── Authentication (Cookie-based) ─────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Login";
    options.Cookie.Name = "PTSA.Monitor";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(
        int.Parse(builder.Configuration["Jwt:ExpiryMinutes"] ?? "480"));
    options.SlidingExpiration = true;
});

// ── Authorization ─────────────────────────────────────────────────────────────
builder.Services.AddAuthorization();

// ── System Initialization ─────────────────────────────────────────────────────
//builder.Services.AddHostedService<SystemInitializationService>();

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
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
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
