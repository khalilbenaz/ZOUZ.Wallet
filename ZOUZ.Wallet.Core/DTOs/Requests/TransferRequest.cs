using System.ComponentModel.DataAnnotations;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class TransferRequest
{
    [Required]
    public Guid SourceWalletId { get; set; }
            
    [Required]
    public Guid DestinationWalletId { get; set; }
            
    [Required]
    public decimal Amount { get; set; }
            
    public string Description { get; set; }
            
    // Pour 2FA
    public string OtpCode { get; set; }
}