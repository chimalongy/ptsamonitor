using Microsoft.EntityFrameworkCore;
using ptsamonitor.Data;
using ptsamonitor.Models;

namespace ptsamonitor.Services;

public class SystemInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemInitializationService> _logger;

    public SystemInitializationService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SystemInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // ── 1. Create default institution: UnifiedPayments ──
            var unifiedPayments = await db.Institutions
                .FirstOrDefaultAsync(i => i.InstitutionName == "UnifiedPayments", cancellationToken);

            if (unifiedPayments is null)
            {
                unifiedPayments = new Institution
                {
                    InstitutionName = "UnifiedPayments",
                    InstitutionType = "Non Bank",
                    InstitutionShortName = "UP",
                    InstitutionEmails = "admin@unifiedpayments.com",
                    InstitutionDomain = "unifiedpayments.com",
                    InstitutionCode = "UP001",
                    CreatedAt = DateTime.UtcNow
                };

                db.Institutions.Add(unifiedPayments);
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created default institution: UnifiedPayments");
            }

            // ── 2. Create system admin user ──
            var adminUser = await db.PtsaUsers
                .FirstOrDefaultAsync(u => u.UserName == "admin", cancellationToken);

            if (adminUser is null)
            {
                var defaultPassword = _configuration["NEW_PASSWORD"];
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                adminUser = new PtsaUser
                {
                    UserName = "admin",
                    Email = "admin@unifiedpayments.com",
                    Password = hashedPassword,
                    Institution = "UnifiedPayments",
                    UserType = "Admin",
                    Privileges = "transaction spooling,transaction moderator",
                    Status = "enabled",
                    CreationDate = DateTime.UtcNow
                };

                db.PtsaUsers.Add(adminUser);
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created system admin user: admin");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system defaults");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
