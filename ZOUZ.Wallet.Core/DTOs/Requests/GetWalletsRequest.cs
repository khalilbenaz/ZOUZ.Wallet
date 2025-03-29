using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class GetWalletsRequest
{
    public string OwnerName { get; set; }
    public Guid? OfferId { get; set; }
    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }
    public WalletStatus? Status { get; set; }
    public KycLevel? KycLevel { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}