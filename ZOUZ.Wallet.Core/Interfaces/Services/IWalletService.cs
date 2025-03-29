using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;

namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface IWalletService
{
    Task<WalletResponse> CreateWalletAsync(CreateWalletRequest request, string userId);
    Task<WalletResponse> GetWalletByIdAsync(Guid id, string userId = null);
    Task<WalletResponse> UpdateWalletAsync(Guid id, UpdateWalletRequest request, string userId = null);
    Task<bool> DeleteWalletAsync(Guid id, string userId = null);
    Task<PagedResponse<WalletResponse>> GetWalletsAsync(GetWalletsRequest request, string userId = null);
    Task<WalletResponse> AssignOfferToWalletAsync(Guid walletId, AssignOfferToWalletRequest request, string userId = null);
    Task<decimal> GetWalletBalanceAsync(Guid walletId, string userId = null);
    Task<bool> CanPerformTransactionAsync(Guid walletId, decimal amount, string userId = null);
}