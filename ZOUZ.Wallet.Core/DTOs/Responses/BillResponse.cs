namespace ZOUZ.Wallet.Core.DTOs.Responses;

public class BillResponse
{
    public Guid Id { get; set; }
    public string BillerName { get; set; }
    public string BillerReference { get; set; }
    public string CustomerReference { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string BillType { get; set; }
}