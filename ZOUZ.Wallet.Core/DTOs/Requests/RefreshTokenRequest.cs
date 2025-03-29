namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class RefreshTokenRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}