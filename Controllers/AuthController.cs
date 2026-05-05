using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ptsamonitor.Classes.Utils;
using ptsamonitor.Data;
using ptsamonitor.Models;

namespace ptsamonitor.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── GET /Auth/Login ───────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    // ── POST /Auth/Login ──────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email and password are required.");
            return View();
        }

        var user = await _db.PtsaUsers
            .FirstOrDefaultAsync(u => u.Email == username.Trim());

        // Generic message - don't reveal whether email exists
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }

        // ── Account must be enabled ───────────────────────────────────────
        if (!string.Equals(user.Status, "enabled", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Your account has been disabled. Please contact your administrator.");
            return View();
        }

        // ── Default-password check -> force change ─────────────────────────
        var defaultPassword = _config["NEW_PASSWORD"];
        if (!string.IsNullOrEmpty(defaultPassword) &&
            BCrypt.Net.BCrypt.Verify(defaultPassword, user.Password))
        {
            TempData["ForceChangeUserId"] = user.Id;
            return RedirectToAction("UpdatePassword");
        }

        // ── All checks passed — sign the user in ──────────────────────────
        await SignInUserAsync(user);

        // Update last-login timestamp
        user.LastLoginDate = DateTime.UtcNow;
        user.LastLoginIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _db.SaveChangesAsync();

        await AuditLogger.LogAsync(
            db: _db,
            eventName: $"{user.UserName} - LOGIN SUCCESSFUL",
            userId: user.Id,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        return RedirectToAction("Index", "Dashboard");
    }

    // ── GET /Auth/UpdatePassword ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> UpdatePassword()
    {
        if (TempData["ForceChangeUserId"] is null)
            return RedirectToAction("Login");

        TempData.Keep("ForceChangeUserId");

        var userId = (int)TempData.Peek("ForceChangeUserId")!;
        var user = await _db.PtsaUsers.FindAsync(userId);

        if (user is null)
            return RedirectToAction("Login");

        ViewBag.UserName = user.UserName;
        return View();
    }

    // ── POST /Auth/UpdatePassword ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        if (TempData["ForceChangeUserId"] is not int userId)
            return RedirectToAction("Login");

        TempData["ForceChangeUserId"] = userId;

        var user = await _db.PtsaUsers.FindAsync(userId);
        if (user is null)
            return RedirectToAction("Login");

        ViewBag.UserName = user.UserName;

        // ── Validate old password ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(oldPassword) || !BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
        {
            ModelState.AddModelError("", "Current password is incorrect.");
            return View();
        }

        // ── Validate new password ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            ModelState.AddModelError("", "New password must be at least 8 characters.");
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match.");
            return View();
        }

        // ── Prevent reusing the default/temporary password ────────────────
        var defaultPassword = _config["NEW_PASSWORD"];
        if (!string.IsNullOrEmpty(defaultPassword) && newPassword == defaultPassword)
        {
            ModelState.AddModelError("", "You cannot reuse the temporary password. Please choose a new one.");
            return View();
        }

        // ── Save ──────────────────────────────────────────────────────────
        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        await AuditLogger.LogAsync(
            db: _db,
            eventName: "PASSWORD UPDATED",
            userId: user.Id,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            pageUrl: HttpContext.Request.Path
        );

        TempData["SuccessMessage"] = "Password updated successfully. Please sign in with your new password.";
        return RedirectToAction("Login");
    }

    // ── GET /Auth/Logout ──────────────────────────────────────────────────
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ── Private helper ────────────────────────────────────────────────────
    private async Task SignInUserAsync(PtsaUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, user.UserType ?? "User"),
            new("institution", user.Institution ?? ""),
            new("privileges", user.Privileges ?? ""),
            new("status", user.Status ?? "enabled")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}