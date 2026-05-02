using Microsoft.EntityFrameworkCore;
using ptsamonitor.Data;
using ptsamonitor.Models;
using ptsamonitor.Models.ViewModels;

namespace ptsamonitor.Classes.Utils;

public static class GlobalFunctions
{
    // ══════════════════════════════════════════════════════════════════════
    // USER OPERATIONS
    // ══════════════════════════════════════════════════════════════════════

    public static async Task<IEnumerable<object>> GetAllUsersAsync(AppDbContext db)
    {
        return await db.PtsaUsers
            .OrderByDescending(u => u.CreationDate)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.Institution,
                u.UserType,
                u.Privileges,
                u.Status,
                u.LastLoginDate,
                u.LastLoginIp,
                u.CreationDate
            })
            .ToListAsync();
    }

    public static async Task<(object? Result, string? Error)> CreateUserAsync(
        AppDbContext db,
        IConfiguration config,
        string userName,
        string? email,
        string? institution,
        string? userType,
        string? privileges)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return (null, "Username is required.");
        }

        bool userExists = await db.PtsaUsers
     .CountAsync(u => u.UserName.ToLower() == userName.Trim().ToLower()) > 0;

        if (userExists)
            return (null, "CONFLICT");

        var defaultPassword = config["NEW_PASSWORD"];
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

        var user = new PtsaUser
        {
            UserName = userName.Trim(),
            Email = email?.Trim(),
            Institution = institution?.Trim(),
            UserType = userType?.Trim(),
            Privileges = privileges?.Trim(),
            Status = "enabled",
            Password = hashedPassword,
            CreationDate = DateTime.UtcNow
        };

        db.PtsaUsers.Add(user);
        await db.SaveChangesAsync();

        return (new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.Institution,
            user.UserType,
            user.Privileges,
            user.Status,
            user.LastLoginDate,
            user.LastLoginIp,
            user.CreationDate
        }, null);
    }

    public static async Task<string?> DeleteUserAsync(AppDbContext db, int id)
    {
        var user = await db.PtsaUsers.FindAsync(id);
        if (user is null)
            return "NOT_FOUND";

        db.PtsaUsers.Remove(user);
        await db.SaveChangesAsync();
        return null;
    }

    public static async Task<(object? Result, string? Error)> UpdateUserStatusAsync(
        AppDbContext db,
        int id,
        string? newStatus)
    {
        var allowed = new[] { "enabled", "disabled" };
        if (!allowed.Contains(newStatus?.ToLower()))
            return (null, "Status must be 'enabled' or 'disabled'.");

        var user = await db.PtsaUsers.FindAsync(id);
        if (user is null)
            return (null, "NOT_FOUND");

        user.Status = newStatus!.ToLower();
        await db.SaveChangesAsync();

        return (new { user.Id, user.Status }, null);
    }

    public static async Task<(object? Result, string? Error)> UpdateUserAsync(
        AppDbContext db,
        int id,
        CreateUserRequest req)
    {
        var user = await db.PtsaUsers.FindAsync(id);
        if (user is null)
            return (null, "NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(req.UserName))
            user.UserName = req.UserName.Trim();
        if (req.Email != null)
            user.Email = req.Email.Trim();
        if (req.Institution != null)
            user.Institution = req.Institution.Trim();
        if (req.UserType != null)
            user.UserType = req.UserType.Trim();
        if (req.Privileges != null)
            user.Privileges = req.Privileges.Trim();

        await db.SaveChangesAsync();

        return (new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.Institution,
            user.UserType,
            user.Privileges,
            user.Status,
            user.LastLoginDate,
            user.LastLoginIp,
            user.CreationDate
        }, null);
    }

    // ══════════════════════════════════════════════════════════════════════
    // INSTITUTION OPERATIONS
    // ══════════════════════════════════════════════════════════════════════

    public static async Task<IEnumerable<object>> GetAllInstitutionsAsync(AppDbContext db)
    {
        return await db.Institutions
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.InstitutionId,
                i.InstitutionName,
                i.InstitutionType,
                i.InstitutionEmails,
                i.BankBins,
                i.TerminalIds,
                i.InstitutionDomain,
                i.InstitutionCode,
                i.InstitutionLogo,
                i.InstitutionShortName,
                i.CreatedAt,
                i.InstitutionSubCodes
            })
            .ToListAsync();
    }

    public static async Task<(object? Result, string? Error)> CreateInstitutionAsync(
        AppDbContext db,
        CreateInstitutionRequest req,
        string? logoPath = null)
    {
        if (string.IsNullOrWhiteSpace(req.InstitutionName))
        {
            return (null, "Institution name is required.");
        }

        
        bool exists = await db.Institutions
            .CountAsync(i => i.InstitutionName.ToLower() == req.InstitutionName.Trim().ToLower()) > 0;

        if (exists)
            return (null, "CONFLICT");

        var institution = new Institution
        {
            InstitutionName = req.InstitutionName.Trim(),
            InstitutionType = req.InstitutionType,
            InstitutionEmails = req.InstitutionEmails,
            BankBins = req.BankBins,
            TerminalIds = req.TerminalIds,
            InstitutionDomain = req.InstitutionDomain,
            InstitutionCode = req.InstitutionCode,
            InstitutionLogo = logoPath,
            InstitutionShortName = req.InstitutionShortName,
            InstitutionSubCodes = req.InstitutionSubCodes,
            CreatedAt = DateTime.UtcNow
        };

        db.Institutions.Add(institution);
        await db.SaveChangesAsync();

        return (new
        {
            institution.InstitutionId,
            institution.InstitutionName,
            institution.InstitutionType,
            institution.InstitutionEmails,
            institution.BankBins,
            institution.TerminalIds,
            institution.InstitutionDomain,
            institution.InstitutionCode,
            institution.InstitutionLogo,
            institution.InstitutionShortName,
            institution.CreatedAt,
            institution.InstitutionSubCodes
        }, null);
    }

    public static async Task<string?> DeleteInstitutionAsync(AppDbContext db, int id)
    {
        var institution = await db.Institutions.FindAsync(id);
        if (institution is null)
            return "NOT_FOUND";

        // Check if any users belong to this institution
        var institutionName = institution.InstitutionName;
        var hasUsers = await db.PtsaUsers
    .CountAsync(u => u.Institution == institutionName) > 0;

        if (hasUsers)
            return "HAS_USERS";

        // Delete logo file if exists
        if (!string.IsNullOrEmpty(institution.InstitutionLogo))
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", institution.InstitutionLogo.TrimStart('/'));
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch { /* Non-fatal */ }
        }

        db.Institutions.Remove(institution);
        await db.SaveChangesAsync();
        return null;
    }

    public static async Task<(object? Result, string? Error)> UpdateInstitutionAsync(
        AppDbContext db,
        int id,
        CreateInstitutionRequest req,
        string? logoPath = null)
    {
        var institution = await db.Institutions.FindAsync(id);
        if (institution is null)
            return (null, "NOT_FOUND");

        var oldName = institution.InstitutionName;

        if (!string.IsNullOrWhiteSpace(req.InstitutionName))
            institution.InstitutionName = req.InstitutionName.Trim();
        if (req.InstitutionType != null)
            institution.InstitutionType = req.InstitutionType;
        if (req.InstitutionEmails != null)
            institution.InstitutionEmails = req.InstitutionEmails;
        if (req.BankBins != null)
            institution.BankBins = req.BankBins;
        if (req.TerminalIds != null)
            institution.TerminalIds = req.TerminalIds;
        if (req.InstitutionDomain != null)
            institution.InstitutionDomain = req.InstitutionDomain;
        if (req.InstitutionCode != null)
            institution.InstitutionCode = req.InstitutionCode;
        if (req.InstitutionShortName != null)
            institution.InstitutionShortName = req.InstitutionShortName;
        if (req.InstitutionSubCodes != null)
            institution.InstitutionSubCodes = req.InstitutionSubCodes;
        if (logoPath != null)
            institution.InstitutionLogo = logoPath;

        await db.SaveChangesAsync();

        // Update users' institution name if institution name changed
        if (oldName != institution.InstitutionName)
        {
            var users = await db.PtsaUsers
                .Where(u => u.Institution == oldName)
                .ToListAsync();

            foreach (var user in users)
            {
                user.Institution = institution.InstitutionName;
            }

            await db.SaveChangesAsync();
        }

        return (new
        {
            institution.InstitutionId,
            institution.InstitutionName,
            institution.InstitutionType,
            institution.InstitutionEmails,
            institution.BankBins,
            institution.TerminalIds,
            institution.InstitutionDomain,
            institution.InstitutionCode,
            institution.InstitutionLogo,
            institution.InstitutionShortName,
            institution.CreatedAt,
            institution.InstitutionSubCodes
        }, null);
    }

    // ══════════════════════════════════════════════════════════════════════
    // AUDIT LOG OPERATIONS
    // ══════════════════════════════════════════════════════════════════════

    public static async Task<IEnumerable<object>> GetAllAuditLogsAsync(AppDbContext db)
    {
        return await db.AuditLogs
            .OrderByDescending(l => l.EventDate)
            .Select(l => new
            {
                l.Id,
                l.UserId,
                l.IpAddress,
                l.Event,
                l.EventDate,
                l.PageUrl
            })
            .ToListAsync();
    }
}
