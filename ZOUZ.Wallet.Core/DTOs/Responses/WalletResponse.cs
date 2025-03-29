using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Responses;

public class WalletResponse
{
    public Guid Id { get; set; }
    public string OwnerName { get; set; }
    public string PhoneNumber { get; set; }
    public decimal Balance { get; set; }
    public CurrencyType Currency { get; set; }
    public WalletStatus Status { get; set; }
    public KycLevel KycLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public decimal CurrentDailyUsage { get; set; }
    public decimal CurrentMonthlyUsage { get; set; }
    public OfferResponse Offer { get; set; }
}