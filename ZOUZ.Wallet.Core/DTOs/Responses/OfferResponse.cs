using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Responses;

public class OfferResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public OfferType Type { get; set; }
    public decimal SpendingLimit { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; }
    public decimal? CashbackPercentage { get; set; }
    public decimal? FeesDiscount { get; set; }
    public decimal? RechargeBonus { get; set; }
}