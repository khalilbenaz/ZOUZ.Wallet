namespace ZOUZ.Wallet.Core.Entities;

public class Bill : BaseEntity
{
    public string BillerName { get; set; } // Par exemple: Maroc Telecom, REDAL, LYDEC
    public string BillerReference { get; set; } // Numéro de référence de la facture
    public string CustomerReference { get; set; } // Identifiant du client chez le fournisseur
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
        
    // Type de facture
    public string BillType { get; set; } // Telecom, Eau, Électricité, Taxes, etc.
        
    // Relations
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}