using System.ComponentModel.DataAnnotations;

namespace ZOUZ.Wallet.Core.DTOs.Requests;

public class VerifyIdentityRequest
{
    [Required]
    public string CinNumber { get; set; }
            
    [Required]
    public string FullName { get; set; }
            
    [Required]
    public DateTime DateOfBirth { get; set; }
            
    public string Address { get; set; }
    public string City { get; set; }
            
    // Base64 encoded image
    public string CinFrontImage { get; set; }
    public string CinBackImage { get; set; }
    public string SelfieImage { get; set; }
}