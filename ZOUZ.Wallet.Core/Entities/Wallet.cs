using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Entities;

public class Wallet: BaseEntity
{
    public string OwnerId { get; set; } // ID de l'utilisateur propriétaire
    public string OwnerName { get; set; }
    public string PhoneNumber { get; set; } // Format: +2126XXXXXXXX
    public decimal Balance { get; set; }
    public CurrencyType Currency { get; set; }
    public WalletStatus Status { get; set; }
    public KycLevel KycLevel { get; set; }
        
    // Limites et plafonds
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public decimal CurrentDailyUsage { get; set; }
    public decimal CurrentMonthlyUsage { get; set; }
        
    // Relations
    public Guid? OfferId { get; set; }
    public Offer Offer { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        
    // Données de vérification KYC
    public string CinNumber { get; set; }
    public bool IsIdentityVerified { get; set; }
    public DateTime? VerificationDate { get; set; }
}