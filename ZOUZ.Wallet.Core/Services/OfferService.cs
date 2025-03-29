using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Exceptions;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Core.Services;

public class OfferService : IOfferService
    {
        private readonly IOfferRepository _offerRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<OfferService> _logger;

        public OfferService(
            IOfferRepository offerRepository,
            IWalletRepository walletRepository,
            ILogger<OfferService> logger)
        {
            _offerRepository = offerRepository;
            _walletRepository = walletRepository;
            _logger = logger;
        }

        public async Task<OfferResponse> CreateOfferAsync(CreateOfferRequest request)
        {
            _logger.LogInformation("Creating new offer: {OfferName}", request.Name);

            // Validation
            ValidateOfferRequest(request);

            var offer = new Offer
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                SpendingLimit = request.SpendingLimit,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                IsActive = true,
                CashbackPercentage = request.CashbackPercentage,
                FeesDiscount = request.FeesDiscount,
                RechargeBonus = request.RechargeBonus,
                CreatedAt = DateTime.UtcNow
            };

            await _offerRepository.AddAsync(offer);
            await _offerRepository.SaveChangesAsync();

            return MapToOfferResponse(offer);
        }

        public async Task<OfferResponse> GetOfferByIdAsync(Guid id)
        {
            var offer = await _offerRepository.GetByIdAsync(id);
            
            if (offer == null)
            {
                throw new NotFoundException($"L'offre avec l'ID {id} n'existe pas.");
            }

            return MapToOfferResponse(offer);
        }

        public async Task<OfferResponse> UpdateOfferAsync(Guid id, CreateOfferRequest request)
        {
            _logger.LogInformation("Updating offer {OfferId}", id);

            var offer = await _offerRepository.GetByIdAsync(id);
            
            if (offer == null)
            {
                throw new NotFoundException($"L'offre avec l'ID {id} n'existe pas.");
            }

            // Validation
            ValidateOfferRequest(request);

            // Mettre à jour les propriétés
            offer.Name = request.Name;
            offer.Description = request.Description;
            offer.Type = request.Type;
            offer.SpendingLimit = request.SpendingLimit;
            offer.ValidFrom = request.ValidFrom;
            offer.ValidTo = request.ValidTo;
            offer.CashbackPercentage = request.CashbackPercentage;
            offer.FeesDiscount = request.FeesDiscount;
            offer.RechargeBonus = request.RechargeBonus;
            offer.UpdatedAt = DateTime.UtcNow;

            await _offerRepository.UpdateAsync(offer);
            await _offerRepository.SaveChangesAsync();

            return MapToOfferResponse(offer);
        }

        public async Task<bool> DeleteOfferAsync(Guid id)
        {
            _logger.LogInformation("Deleting offer {OfferId}", id);

            var offer = await _offerRepository.GetByIdAsync(id);
            
            if (offer == null)
            {
                throw new NotFoundException($"L'offre avec l'ID {id} n'existe pas.");
            }

            // Vérifier si l'offre est utilisée par des wallets
            var walletsCount = await _offerRepository.CountWalletsByOfferIdAsync(id);
            if (walletsCount > 0)
            {
                throw new BusinessRuleException($"Impossible de supprimer cette offre car elle est utilisée par {walletsCount} wallet(s).");
            }

            await _offerRepository.DeleteAsync(id);
            await _offerRepository.SaveChangesAsync();

            return true;
        }

        public async Task<PagedResponse<OfferResponse>> GetOffersAsync(
            bool activeOnly = false, 
            OfferType? type = null, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            var offers = await _offerRepository.GetOffersAsync(activeOnly, type, pageNumber, pageSize);
            var totalCount = await _offerRepository.CountOffersAsync(activeOnly, type);

            var offerResponses = offers.Select(MapToOfferResponse).ToList();

            return new PagedResponse<OfferResponse>(
                offerResponses,
                pageNumber,
                pageSize,
                totalCount);
        }

        public async Task<PagedResponse<WalletResponse>> GetWalletsByOfferIdAsync(
            Guid offerId, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            // Vérifier que l'offre existe
            var offer = await _offerRepository.GetByIdAsync(offerId);
            
            if (offer == null)
            {
                throw new NotFoundException($"L'offre avec l'ID {offerId} n'existe pas.");
            }

            var wallets = await _offerRepository.GetWalletsByOfferIdAsync(offerId, pageNumber, pageSize);
            var totalCount = await _offerRepository.CountWalletsByOfferIdAsync(offerId);

            var walletResponses = wallets.Select(MapToWalletResponse).ToList();

            return new PagedResponse<WalletResponse>(
                walletResponses,
                pageNumber,
                pageSize,
                totalCount);
        }

        public async Task<OfferResponse> ActivateOfferAsync(Guid id)
        {
            _logger.LogInformation("Activating offer {OfferId}", id);

            var offer = await _offerRepository.GetByIdAsync(id);
            
            if (offer == null)
            {
                throw new NotFoundException($"L'offre avec l'ID {id} n'existe pas.");
            }

            // Vérifier si l'offre est déjà active
            if (offer.IsActive)
            {
                _logger.LogInformation("Offer {OfferId} is already active", id);
                return MapToOfferResponse(offer);
            }

            // Vérifier si l'offre n'est pas expirée
            if (offer.ValidTo < DateTime.UtcNow)
            {
                throw new BusinessRuleException("Impossible d'activer une offre expirée.");
            }

            offer.IsActive = true;
            offer.UpdatedAt = DateTime.UtcNow;

            await _offerRepository.UpdateAsync(offer);
            await _offerRepository.SaveChangesAsync();

            return MapToOfferResponse(offer);
        }

        public async Task<OfferResponse> DeactivateOfferAsync(Guid id)
        {
            _logger.LogInformation("Deactivating offer {OfferId}", id);

            var offer = await _offerRepository.GetByIdAsync(id);
            
            if (offer == null)
            {
                throw new NotFoundException($"L'offre avec l'ID {id} n'existe pas.");
            }

            // Vérifier si l'offre est déjà inactive
            if (!offer.IsActive)
            {
                _logger.LogInformation("Offer {OfferId} is already inactive", id);
                return MapToOfferResponse(offer);
            }

            offer.IsActive = false;
            offer.UpdatedAt = DateTime.UtcNow;

            await _offerRepository.UpdateAsync(offer);
            await _offerRepository.SaveChangesAsync();

            return MapToOfferResponse(offer);
        }

        // Méthodes privées 
        private void ValidateOfferRequest(CreateOfferRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new ValidationException("Le nom de l'offre est requis.");
            }

            if (string.IsNullOrEmpty(request.Description))
            {
                throw new ValidationException("La description de l'offre est requise.");
            }

            if (request.SpendingLimit <= 0)
            {
                throw new ValidationException("La limite de dépense doit être positive.");
            }

            if (request.ValidFrom >= request.ValidTo)
            {
                throw new ValidationException("La date de début doit être antérieure à la date de fin.");
            }

            // Validations spécifiques selon le type d'offre
            switch (request.Type)
            {
                case OfferType.Cashback:
                    if (!request.CashbackPercentage.HasValue || request.CashbackPercentage <= 0 || request.CashbackPercentage > 100)
                    {
                        throw new ValidationException("Le pourcentage de cashback doit être compris entre 0 et 100.");
                    }
                    break;
                case OfferType.ReducedFees:
                    if (!request.FeesDiscount.HasValue || request.FeesDiscount <= 0 || request.FeesDiscount > 100)
                    {
                        throw new ValidationException("La réduction des frais doit être comprise entre 0 et 100.");
                    }
                    break;
                case OfferType.RechargeBonus:
                    if (!request.RechargeBonus.HasValue || request.RechargeBonus <= 0 || request.RechargeBonus > 100)
                    {
                        throw new ValidationException("Le bonus de recharge doit être compris entre 0 et 100.");
                    }
                    break;
            }
        }

        private OfferResponse MapToOfferResponse(Offer offer)
        {
            return new OfferResponse
            {
                Id = offer.Id,
                Name = offer.Name,
                Description = offer.Description,
                Type = offer.Type,
                SpendingLimit = offer.SpendingLimit,
                ValidFrom = offer.ValidFrom,
                ValidTo = offer.ValidTo,
                IsActive = offer.IsActive,
                CashbackPercentage = offer.CashbackPercentage,
                FeesDiscount = offer.FeesDiscount,
                RechargeBonus = offer.RechargeBonus
            };
        }

        private WalletResponse MapToWalletResponse(Entities.Wallet wallet)
        {
            return new WalletResponse
            {
                Id = wallet.Id,
                OwnerName = wallet.OwnerName,
                PhoneNumber = wallet.PhoneNumber,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                Status = wallet.Status,
                KycLevel = wallet.KycLevel,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt,
                DailyLimit = wallet.DailyLimit,
                MonthlyLimit = wallet.MonthlyLimit,
                CurrentDailyUsage = wallet.CurrentDailyUsage,
                CurrentMonthlyUsage = wallet.CurrentMonthlyUsage
            };
        }
    }