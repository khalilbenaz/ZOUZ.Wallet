using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface IOfferService
{
    Task<OfferResponse> CreateOfferAsync(CreateOfferRequest request);
    Task<OfferResponse> GetOfferByIdAsync(Guid id);
    Task<OfferResponse> UpdateOfferAsync(Guid id, CreateOfferRequest request);
    Task<bool> DeleteOfferAsync(Guid id);
    Task<PagedResponse<OfferResponse>> GetOffersAsync(bool activeOnly = false, OfferType? type = null, int pageNumber = 1, int pageSize = 10);
    Task<PagedResponse<WalletResponse>> GetWalletsByOfferIdAsync(Guid offerId, int pageNumber = 1, int pageSize = 10);
    Task<OfferResponse> ActivateOfferAsync(Guid id);
    Task<OfferResponse> DeactivateOfferAsync(Guid id);
}