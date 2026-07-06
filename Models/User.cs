namespace SolarBackend.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? MfaSecretKey { get; set; }
    public bool IsMfaEnabled { get; set; } = false;
    public string? RecoveryCodes { get; set; }
}
