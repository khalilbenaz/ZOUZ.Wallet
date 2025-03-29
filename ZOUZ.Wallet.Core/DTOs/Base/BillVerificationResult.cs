namespace ZOUZ.Wallet.Core.DTOs.Base;

public class BillVerificationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
    public DateTime DueDate { get; set; }
}