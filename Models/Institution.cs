using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ptsamonitor.Models;

[Table("PTSA_MONITOR_INSTITUTIONS", Schema = "TADEYI")]
public class Institution
{
    [Key]
    [Column("INSTITUTION_ID")]
    public int InstitutionId { get; set; }

    [Column("INSTITUTION_NAME")]
    [StringLength(200)]
    [Required]
    public string InstitutionName { get; set; } = string.Empty;

    [Column("INSTITUTION_TYPE")]
    [StringLength(100)]
    public string? InstitutionType { get; set; }

    [Column("INSTITUTION_EMAILS")]
    [StringLength(500)]
    public string? InstitutionEmails { get; set; }

    [Column("BANK_BINS")]
    [StringLength(500)]
    public string? BankBins { get; set; }

    [Column("TERMINAL_IDS")]
    [StringLength(500)]
    public string? TerminalIds { get; set; }

    [Column("INSTITUTION_DOMAIN")]
    [StringLength(200)]
    public string? InstitutionDomain { get; set; }

    [Column("INSTITUTION_CODE")]
    [StringLength(50)]
    public string? InstitutionCode { get; set; }

    [Column("INSTITUTION_LOGO")]
    [StringLength(500)]
    public string? InstitutionLogo { get; set; }

    [Column("INSTITUTION_SHORT_NAME")]
    [StringLength(100)]
    public string? InstitutionShortName { get; set; }

    [Column("CREATED_AT")]
    public DateTime? CreatedAt { get; set; }

    [Column("INSTITUTION_SUB_CODES")]
    [StringLength(500)]
    public string? InstitutionSubCodes { get; set; }
}
