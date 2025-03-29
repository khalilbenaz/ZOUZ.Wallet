using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class DepositRequest
{
    [Required]
    public decimal Amount { get; set; }
            
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethod PaymentMethod { get; set; }
            
    public string Description { get; set; }
            
    // Pour les paiements par carte
    public string CardNumber { get; set; }
    public string CardHolderName { get; set; }
    public string ExpiryDate { get; set; }
    public string Cvv { get; set; }
            
    // Pour Orange Money/Inwi Money
    public string MobileOperatorReference { get; set; }
}