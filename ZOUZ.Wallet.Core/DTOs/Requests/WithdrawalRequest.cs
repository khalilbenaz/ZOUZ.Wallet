using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class WithdrawalRequest
{
    [Required]
    public decimal Amount { get; set; }
            
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethod PaymentMethod { get; set; }
            
    public string Description { get; set; }
            
    // Pour les retraits vers un compte bancaire
    public string BankAccountNumber { get; set; }
    public string BankName { get; set; }
            
    // Pour les retraits vers un mobile money
    public string RecipientPhoneNumber { get; set; }
}