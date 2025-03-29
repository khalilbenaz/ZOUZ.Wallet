using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Entities;

public class User : BaseEntity
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string CinNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public KycLevel KycLevel { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public string Role { get; set; } // "User", "Admin"
        
    // Relations
    public ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
}