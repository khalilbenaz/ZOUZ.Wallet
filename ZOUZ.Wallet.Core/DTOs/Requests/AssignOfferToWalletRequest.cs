using System.ComponentModel.DataAnnotations;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class AssignOfferToWalletRequest
{
    [Required]
    public Guid OfferId { get; set; }
}