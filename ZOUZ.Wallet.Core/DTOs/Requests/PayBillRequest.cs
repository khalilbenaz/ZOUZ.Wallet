using System.ComponentModel.DataAnnotations;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class PayBillRequest
{
    [Required]
    public string BillerName { get; set; }
            
    [Required]
    public string BillerReference { get; set; }
            
    [Required]
    public string CustomerReference { get; set; }
            
    [Required]
    public decimal Amount { get; set; }
            
    [Required]
    public string BillType { get; set; } // Telecom, Eau, Électricité, Taxes
}