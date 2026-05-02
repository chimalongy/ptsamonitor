namespace ptsamonitor.Models.ViewModels;

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? UserType { get; set; }
    public string? Privileges { get; set; }
}
