using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ptsamonitor.Models;

[Table("PTSA_MONITOR_USERS", Schema = "TADEYI")]
public class PtsaUser
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("USER_NAME")]
    [StringLength(100)]
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Column("PASSWORD")]
    [StringLength(255)]
    [Required]
    public string Password { get; set; } = string.Empty;

    [Column("EMAIL")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Column("INSTITUTION")]
    [StringLength(150)]
    public string? Institution { get; set; }

    [Column("USER_TYPE")]
    [StringLength(50)]
    public string? UserType { get; set; }

    [Column("PRIVILEGES")]
    [StringLength(500)]
    public string? Privileges { get; set; }

    [Column("STATUS")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("LAST_LOGIN_DATE")]
    public DateTime? LastLoginDate { get; set; }

    [Column("LAST_LOGIN_IP")]
    [StringLength(45)]
    public string? LastLoginIp { get; set; }

    [Column("CREATION_DATE")]
    public DateTime? CreationDate { get; set; }
}
