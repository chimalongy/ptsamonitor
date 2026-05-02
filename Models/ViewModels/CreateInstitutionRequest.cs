namespace ptsamonitor.Models.ViewModels;

public class CreateInstitutionRequest
{
    public string InstitutionName { get; set; } = string.Empty;
    public string? InstitutionType { get; set; }
    public string? InstitutionEmails { get; set; }
    public string? BankBins { get; set; }
    public string? TerminalIds { get; set; }
    public string? InstitutionDomain { get; set; }
    public string? InstitutionCode { get; set; }
    public string? InstitutionShortName { get; set; }
    public string? InstitutionSubCodes { get; set; }
}
