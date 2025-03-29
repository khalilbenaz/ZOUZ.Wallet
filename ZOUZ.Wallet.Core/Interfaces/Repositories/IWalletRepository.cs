using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<Entities.Wallet> GetByIdAsync(Guid id);
    Task<IEnumerable<Entities.Wallet>> GetAllAsync();
    Task<IEnumerable<Entities.Wallet>> GetWalletsAsync(string ownerId = null, string ownerName = null, Guid? offerId = null, decimal? minBalance = null, decimal? maxBalance = null, WalletStatus? status = null, KycLevel? kycLevel = null, int pageNumber = 1, int pageSize = 10, bool includeOffers = false);
    Task<int> CountWalletsAsync(string ownerId = null, string ownerName = null, Guid? offerId = null, decimal? minBalance = null, decimal? maxBalance = null, WalletStatus? status = null, KycLevel? kycLevel = null);
    Task<DateTime?> GetLastTransactionDateAsync(Guid walletId);
    Task<bool> HasActiveTransactionsAsync(Guid walletId);
    Task AddAsync(Entities.Wallet wallet);
    Task UpdateAsync(Entities.Wallet wallet);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}