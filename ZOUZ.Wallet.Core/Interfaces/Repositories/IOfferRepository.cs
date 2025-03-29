using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Interfaces.Repositories;

public interface IOfferRepository
{
    Task<Offer> GetByIdAsync(Guid id);
    Task<IEnumerable<Offer>> GetAllAsync();
    Task<IEnumerable<Offer>> GetActiveOffersAsync();
    Task<IEnumerable<Offer>> GetOffersByTypeAsync(OfferType type);
    Task<IEnumerable<Offer>> GetOffersAsync(bool activeOnly = false, OfferType? type = null, int pageNumber = 1, int pageSize = 10);
    Task<int> CountOffersAsync(bool activeOnly = false, OfferType? type = null);
    Task<IEnumerable<Entities.Wallet>> GetWalletsByOfferIdAsync(Guid offerId, int pageNumber = 1, int pageSize = 10);
    Task<int> CountWalletsByOfferIdAsync(Guid offerId);
    Task AddAsync(Offer offer);
    Task UpdateAsync(Offer offer);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}