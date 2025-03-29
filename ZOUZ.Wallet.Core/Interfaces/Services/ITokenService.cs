using System.Security.Claims;
using ZOUZ.Wallet.Core.Entities;

namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface ITokenService
{
    string GenerateJwtToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}