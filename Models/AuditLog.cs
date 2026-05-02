using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ptsamonitor.Models;

[Table("PTSA_MONITOR_AUDIT_LOGS", Schema = "TADEYI")]
public class AuditLog
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("EVENT_NAME")]
    [StringLength(500)]
    public string? Event { get; set; }

    [Column("USER_ID")]
    public int? UserId { get; set; }

    [Column("IP_ADDRESS")]
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [Column("PAGE_URL")]
    [StringLength(500)]
    public string? PageUrl { get; set; }

    [Column("EVENT_DATE")]
    public DateTime? EventDate { get; set; }
}
