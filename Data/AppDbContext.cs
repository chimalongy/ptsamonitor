using Microsoft.EntityFrameworkCore;
using ptsamonitor.Models;

namespace ptsamonitor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PtsaUser> PtsaUsers { get; set; }
    public DbSet<Institution> Institutions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PtsaUser>(entity =>
        {
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.Property(u => u.Privileges)
                  .HasColumnName("PRIVILEGES");
        });

        base.OnModelCreating(modelBuilder);
    }
}
