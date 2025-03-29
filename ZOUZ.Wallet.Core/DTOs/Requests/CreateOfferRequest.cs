using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class CreateOfferRequest
{
    [Required]
    public string Name { get; set; }
            
    [Required]
    public string Description { get; set; }
            
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OfferType Type { get; set; }
            
    [Required]
    public decimal SpendingLimit { get; set; }
            
    [Required]
    public DateTime ValidFrom { get; set; }
            
    [Required]
    public DateTime ValidTo { get; set; }
            
    // Avantages spécifiques selon le type d'offre
    public decimal? CashbackPercentage { get; set; }
    public decimal? FeesDiscount { get; set; }
    public decimal? RechargeBonus { get; set; }
}