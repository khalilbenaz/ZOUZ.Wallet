namespace ZOUZ.Wallet.Core.DTOs.Responses;

public class AuthResponse
{
    public bool Success { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime Expiration { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}