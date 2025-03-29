using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class CreateWalletRequest
{
    [Required]
    public string OwnerName { get; set; }
            
    [Required]
    [RegularExpression(@"^\+2126\d{8}$", ErrorMessage = "Le numéro doit être au format +2126XXXXXXXX")]
    public string PhoneNumber { get; set; }
            
    [Required]
    public decimal InitialBalance { get; set; }
            
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CurrencyType Currency { get; set; } = CurrencyType.MAD;
            
    public Guid? OfferId { get; set; }
            
    // Données KYC optionnelles
    public string CinNumber { get; set; }
    public string Email { get; set; }
}