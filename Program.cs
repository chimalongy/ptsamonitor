using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using ptsamonitor.Classes.Utils;
using ptsamonitor.Data;
using ptsamonitor.Services;
using ptsamonitor.Workers;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

string connstring = "User Id=PTSA;Password=ptsa1234;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));";

string encryptedConnstring = Cryptor.Encrypt(connstring, true);




// ── Database ──────────────────────────────────────────────────────────────────
string? encryptedUsersConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(encryptedUsersConnectionString))
    throw new Exception("Connection string 'DefaultConnection' not found.");

string decryptedUsersConnectionString = Cryptor.Decrypt(encryptedUsersConnectionString, true);
// Validate Oracle connection string format
if (!decryptedUsersConnectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Invalid Oracle connection string. Expected 'Data Source' parameter.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(decryptedUsersConnectionString, opts =>
        opts.MigrationsHistoryTable("__EFMigrationsHistory", "TADEYI")));



// Register Redis as a singleton — one connection shared across the app
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(connStr);
});

builder.Services.AddSingleton<DashboardCacheService>();
builder.Services.AddHostedService<CacheRefreshWorker>();







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
