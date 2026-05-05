using ptsamonitor.Data;
using ptsamonitor.Models;

namespace ptsamonitor.Classes.Utils;

public static class AuditLogger
{
    public static async Task LogAsync(
        AppDbContext db,
        string eventName,
        int? userId = null,
        string? ipAddress = null,
        string? pageUrl = null)
    {
        var log = new AuditLog
        {
            Event = eventName,
            UserId = userId,
            IpAddress = ipAddress,
            PageUrl = pageUrl,
            EventDate = DateTime.UtcNow
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
