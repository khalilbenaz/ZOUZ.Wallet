using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Base;

public class KycVerificationStatus
{
    public bool IsVerified { get; set; }
    public KycLevel CurrentLevel { get; set; }
    public string Message { get; set; }
    public DateTime? VerificationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}