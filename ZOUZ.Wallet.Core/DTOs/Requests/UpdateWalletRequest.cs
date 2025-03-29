using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class UpdateWalletRequest
{
    public string OwnerName { get; set; }
    public string PhoneNumber { get; set; }
    public Guid? OfferId { get; set; }
    public WalletStatus? Status { get; set; }
    public KycLevel? KycLevel { get; set; }
    public string CinNumber { get; set; }
}