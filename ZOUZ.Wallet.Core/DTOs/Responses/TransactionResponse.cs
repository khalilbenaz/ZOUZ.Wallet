using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Responses;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid? DestinationWalletId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal? Cashback { get; set; }
    public string Description { get; set; }
    public string ReferenceNumber { get; set; }
    public bool IsSuccessful { get; set; }
    public string FailureReason { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
            
    // Pour les paiements de factures
    public BillResponse Bill { get; set; }
}