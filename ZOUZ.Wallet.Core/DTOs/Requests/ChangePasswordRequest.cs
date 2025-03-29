namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class ChangePasswordRequest
{
    
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmNewPassword { get; set; }
}