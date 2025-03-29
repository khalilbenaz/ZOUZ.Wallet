using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Exceptions;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Core.Interfaces.Services;
using ValidationException = ZOUZ.Wallet.Core.Exceptions.ValidationException;

namespace ZOUZ.Wallet.Core.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IKycService _kycService;
    private readonly ILogger<WalletService> _logger;
    
    public WalletService(
        IWalletRepository walletRepository,
        IOfferRepository offerRepository,
        IKycService kycService,
        ILogger<WalletService> logger)
    {
        _walletRepository = walletRepository;
        _offerRepository = offerRepository;
        _kycService = kycService;
        _logger = logger;
    }
    
    public async Task<WalletResponse> CreateWalletAsync(CreateWalletRequest request, string userId)
        {
            _logger.LogInformation("Creating new wallet for user {UserId}", userId);

            // Vérifier l'offre si elle est spécifiée
            if (request.OfferId.HasValue)
            {
                var offer = await _offerRepository.GetByIdAsync(request.OfferId.Value);
                if (offer == null)
                {
                    throw new NotFoundException($"L'offre avec l'ID {request.OfferId} n'existe pas.");
                }

                if (!offer.IsActive || offer.ValidTo < DateTime.UtcNow)
                {
                    throw new BusinessRuleException("L'offre spécifiée n'est pas active ou a expiré.");
                }
            }

            // Vérifier le format du numéro de téléphone marocain
            if (!IsValidMoroccanPhoneNumber(request.PhoneNumber))
            {
                throw new ValidationException("Le numéro de téléphone doit être au format +2126XXXXXXXX");
            }

            // Déterminer le niveau KYC initial
            var kycLevel = KycLevel.None;
            
            if (!string.IsNullOrEmpty(request.CinNumber))
            {
                // Vérification basique du format CIN
                if (IsValidCinFormat(request.CinNumber))
                {
                    kycLevel = KycLevel.Basic;
                }
            }

            // Créer le wallet avec les configurations appropriées selon le niveau KYC
            var wallet = new Entities.Wallet
            {
                Id = Guid.NewGuid(),
                OwnerId = userId,
                OwnerName = request.OwnerName,
                PhoneNumber = request.PhoneNumber,
                Balance = request.InitialBalance,
                Currency = request.Currency,
                Status = WalletStatus.Active,
                KycLevel = kycLevel,
                OfferId = request.OfferId,
                CreatedAt = DateTime.UtcNow,
                CinNumber = request.CinNumber,
                IsIdentityVerified = false,
                
                // Définir les limites en fonction du niveau KYC
                // Conforme aux règles de Bank Al-Maghrib pour les portefeuilles électroniques
                DailyLimit = kycLevel switch
                {
                    KycLevel.None => 1000m, // 1000 MAD pour les utilisateurs non vérifiés
                    KycLevel.Basic => 5000m, // 5000 MAD pour les utilisateurs avec vérification basique
                    KycLevel.Standard => 10000m, // 10000 MAD pour les utilisateurs avec CIN vérifié
                    KycLevel.Advanced => 20000m, // 20000 MAD pour les utilisateurs entièrement vérifiés
                    _ => 1000m
                },
                
                MonthlyLimit = kycLevel switch
                {
                    KycLevel.None => 5000m,
                    KycLevel.Basic => 20000m,
                    KycLevel.Standard => 50000m,
                    KycLevel.Advanced => 100000m,
                    _ => 5000m
                },
                
                CurrentDailyUsage = 0m,
                CurrentMonthlyUsage = 0m
            };

            // Enregistrer le wallet
            await _walletRepository.AddAsync(wallet);
            await _walletRepository.SaveChangesAsync();

            // Si CIN fourni, déclencher une vérification asynchrone
            if (!string.IsNullOrEmpty(request.CinNumber))
            {
                // Cette partie serait idéalement gérée par un système de messaging/queue
                // pour un traitement asynchrone
                _logger.LogInformation("Initiating asynchronous KYC verification for wallet {WalletId}", wallet.Id);
                
                // Dans un scénario réel, on enverrait cela à une file d'attente pour traitement asynchrone
                // Pour l'exemple, nous allons simplement appeler la méthode directement
                await _kycService.InitiateBasicVerificationAsync(wallet.Id, request.CinNumber);
            }

            // Mapper vers la réponse
            var response = new WalletResponse
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

            if (wallet.OfferId.HasValue)
            {
                var offer = await _offerRepository.GetByIdAsync(wallet.OfferId.Value);
                if (offer != null)
                {
                    response.Offer = new OfferResponse
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
            }

            return response;
        }
    public async Task<WalletResponse> GetWalletByIdAsync(Guid id, string userId = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(id);
            
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {id} n'existe pas.");
            }

            // Vérification de propriété si userId est fourni (sécurité)
            if (userId != null && wallet.OwnerId != userId)
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à accéder à ce wallet.");
            }

            // Réinitialiser les limites quotidiennes si nécessaire
            await CheckAndResetDailyLimitsAsync(wallet);

            // Mapper vers la réponse
            var response = new WalletResponse
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

            if (wallet.OfferId.HasValue)
            {
                var offer = await _offerRepository.GetByIdAsync(wallet.OfferId.Value);
                if (offer != null)
                {
                    response.Offer = new OfferResponse
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
            }

            return response;
        }

        public async Task<WalletResponse> UpdateWalletAsync(Guid id, UpdateWalletRequest request, string userId = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(id);
            
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {id} n'existe pas.");
            }

            // Vérification de propriété si userId est fourni (sécurité)
            if (userId != null && wallet.OwnerId != userId)
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à modifier ce wallet.");
            }

            // Mettre à jour les propriétés si fournies
            if (!string.IsNullOrEmpty(request.OwnerName))
            {
                wallet.OwnerName = request.OwnerName;
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                if (!IsValidMoroccanPhoneNumber(request.PhoneNumber))
                {
                    throw new ValidationException("Le numéro de téléphone doit être au format +2126XXXXXXXX");
                }
                wallet.PhoneNumber = request.PhoneNumber;
            }

            if (request.OfferId.HasValue)
            {
                var offer = await _offerRepository.GetByIdAsync(request.OfferId.Value);
                if (offer == null)
                {
                    throw new NotFoundException($"L'offre avec l'ID {request.OfferId} n'existe pas.");
                }

                if (!offer.IsActive || offer.ValidTo < DateTime.UtcNow)
                {
                    throw new BusinessRuleException("L'offre spécifiée n'est pas active ou a expiré.");
                }

                wallet.OfferId = request.OfferId;
            }

            if (request.Status.HasValue)
            {
                // Vérifier si c'est un admin qui fait la mise à jour (nécessaire pour certains statuts)
                if (userId != null && !await IsUserAdminAsync(userId) && 
                    (request.Status == WalletStatus.Blocked || request.Status == WalletStatus.Inactive))
                {
                    throw new UnauthorizedException("Seul un administrateur peut bloquer ou désactiver un wallet.");
                }

                wallet.Status = request.Status.Value;
            }

            if (request.KycLevel.HasValue)
            {
                // Vérifier si c'est un admin qui fait la mise à jour (nécessaire pour changer le niveau KYC)
                if (userId != null && !await IsUserAdminAsync(userId))
                {
                    throw new UnauthorizedException("Seul un administrateur peut modifier le niveau KYC.");
                }

                wallet.KycLevel = request.KycLevel.Value;
                
                // Mise à jour des limites en fonction du nouveau niveau KYC
                wallet.DailyLimit = request.KycLevel.Value switch
                {
                    KycLevel.None => 1000m,
                    KycLevel.Basic => 5000m,
                    KycLevel.Standard => 10000m,
                    KycLevel.Advanced => 20000m,
                    _ => 1000m
                };
                
                wallet.MonthlyLimit = request.KycLevel.Value switch
                {
                    KycLevel.None => 5000m,
                    KycLevel.Basic => 20000m,
                    KycLevel.Standard => 50000m,
                    KycLevel.Advanced => 100000m,
                    _ => 5000m
                };
            }

            if (!string.IsNullOrEmpty(request.CinNumber) && wallet.CinNumber != request.CinNumber)
            {
                if (!IsValidCinFormat(request.CinNumber))
                {
                    throw new ValidationException("Le format du CIN est invalide.");
                }
                
                wallet.CinNumber = request.CinNumber;
                wallet.IsIdentityVerified = false; // Réinitialiser le statut de vérification
                
                // Démarrer une nouvelle vérification si CIN est modifié
                await _kycService.InitiateBasicVerificationAsync(wallet.Id, request.CinNumber);
            }

            // Mettre à jour le timestamp
            wallet.UpdatedAt = DateTime.UtcNow;

            // Enregistrer les modifications
            await _walletRepository.UpdateAsync(wallet);
            await _walletRepository.SaveChangesAsync();

            // Retourner le wallet mis à jour
            return await GetWalletByIdAsync(id, userId);
        }

        public async Task<bool> DeleteWalletAsync(Guid id, string userId = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(id);
            
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {id} n'existe pas.");
            }

            // Vérification de propriété ou admin si userId est fourni
            if (userId != null && wallet.OwnerId != userId && !await IsUserAdminAsync(userId))
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à supprimer ce wallet.");
            }

            // Vérifier si le wallet a un solde
            if (wallet.Balance > 0)
            {
                throw new BusinessRuleException("Impossible de supprimer un wallet avec un solde positif. Veuillez d'abord retirer tout le solde.");
            }

            // Vérifier s'il existe des transactions actives
            var hasActiveTransactions = await _walletRepository.HasActiveTransactionsAsync(id);
            if (hasActiveTransactions)
            {
                throw new BusinessRuleException("Impossible de supprimer un wallet avec des transactions actives ou récentes.");
            }

            // Supprimer le wallet
            await _walletRepository.DeleteAsync(id);
            await _walletRepository.SaveChangesAsync();

            return true;
        }

        public async Task<PagedResponse<WalletResponse>> GetWalletsAsync(GetWalletsRequest request, string userId = null)
        {
            // Si userId est fourni, récupérer uniquement les wallets de l'utilisateur sauf si c'est un admin
            bool isAdmin = userId != null && await IsUserAdminAsync(userId);
            
            // Construire la requête de filtrage
            var wallets = await _walletRepository.GetWalletsAsync(
                ownerId: !isAdmin && userId != null ? userId : null,
                ownerName: request.OwnerName,
                offerId: request.OfferId,
                minBalance: request.MinBalance,
                maxBalance: request.MaxBalance,
                status: request.Status,
                kycLevel: request.KycLevel,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                includeOffers: true);

            // Récupérer le nombre total d'enregistrements pour la pagination
            var totalRecords = await _walletRepository.CountWalletsAsync(
                ownerId: !isAdmin && userId != null ? userId : null,
                ownerName: request.OwnerName,
                offerId: request.OfferId,
                minBalance: request.MinBalance,
                maxBalance: request.MaxBalance,
                status: request.Status,
                kycLevel: request.KycLevel);

            // Mapper vers les réponses
            var walletResponses = wallets.Select(w => new WalletResponse
            {
                Id = w.Id,
                OwnerName = w.OwnerName,
                PhoneNumber = w.PhoneNumber,
                Balance = w.Balance,
                Currency = w.Currency,
                Status = w.Status,
                KycLevel = w.KycLevel,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                DailyLimit = w.DailyLimit,
                MonthlyLimit = w.MonthlyLimit,
                CurrentDailyUsage = w.CurrentDailyUsage,
                CurrentMonthlyUsage = w.CurrentMonthlyUsage,
                Offer = w.Offer != null ? new OfferResponse
                {
                    Id = w.Offer.Id,
                    Name = w.Offer.Name,
                    Description = w.Offer.Description,
                    Type = w.Offer.Type,
                    SpendingLimit = w.Offer.SpendingLimit,
                    ValidFrom = w.Offer.ValidFrom,
                    ValidTo = w.Offer.ValidTo,
                    IsActive = w.Offer.IsActive,
                    CashbackPercentage = w.Offer.CashbackPercentage,
                    FeesDiscount = w.Offer.FeesDiscount,
                    RechargeBonus = w.Offer.RechargeBonus
                } : null
            }).ToList();

            return new PagedResponse<WalletResponse>(
                walletResponses,
                request.PageNumber,
                request.PageSize,
                totalRecords);
        }

        public async Task<WalletResponse> AssignOfferToWalletAsync(Guid walletId, AssignOfferToWalletRequest request, string userId = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {walletId} n'existe pas.");
            }

            // Vérification de propriété si userId est fourni
            if (userId != null && wallet.OwnerId != userId && !await IsUserAdminAsync(userId))
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à modifier ce wallet.");
            }

            var offer = await _offerRepository.GetByIdAsync(request.OfferId);
            if (offer == null)
            {
                throw new NotFoundException($"L'offre avec l'ID {request.OfferId} n'existe pas.");
            }

            if (!offer.IsActive || offer.ValidTo < DateTime.UtcNow)
            {
                throw new BusinessRuleException("L'offre spécifiée n'est pas active ou a expiré.");
            }

            // Assigner l'offre au wallet
            wallet.OfferId = request.OfferId;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Enregistrer les modifications
            await _walletRepository.UpdateAsync(wallet);
            await _walletRepository.SaveChangesAsync();

            // Retourner le wallet mis à jour
            return await GetWalletByIdAsync(walletId, userId);
        }

        public async Task<decimal> GetWalletBalanceAsync(Guid walletId, string userId = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {walletId} n'existe pas.");
            }

            // Vérification de propriété si userId est fourni
            if (userId != null && wallet.OwnerId != userId && !await IsUserAdminAsync(userId))
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à accéder à ce wallet.");
            }

            return wallet.Balance;
        }

        // Vérifier si un wallet peut effectuer une transaction en fonction de son état et de ses limites
        public async Task<bool> CanPerformTransactionAsync(Guid walletId, decimal amount, string userId = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {walletId} n'existe pas.");
            }

            // Vérification de propriété si userId est fourni
            if (userId != null && wallet.OwnerId != userId && !await IsUserAdminAsync(userId))
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à effectuer des opérations sur ce wallet.");
            }

            // Vérifier l'état du wallet
            if (wallet.Status != WalletStatus.Active)
            {
                throw new BusinessRuleException($"Le wallet est actuellement {wallet.Status}. Seuls les wallets actifs peuvent effectuer des transactions.");
            }

            // Pour les retraits, vérifier que le solde est suffisant
            if (amount > wallet.Balance)
            {
                throw new InsufficientBalanceException("Solde insuffisant pour effectuer cette transaction.");
            }

            // Réinitialiser les limites quotidiennes si nécessaire
            await CheckAndResetDailyLimitsAsync(wallet);

            // Vérifier les limites quotidiennes
            if (wallet.CurrentDailyUsage + amount > wallet.DailyLimit)
            {
                throw new BusinessRuleException($"Cette transaction dépasserait votre limite quotidienne de {wallet.DailyLimit} {wallet.Currency}.");
            }

            // Vérifier les limites mensuelles
            if (wallet.CurrentMonthlyUsage + amount > wallet.MonthlyLimit)
            {
                throw new BusinessRuleException($"Cette transaction dépasserait votre limite mensuelle de {wallet.MonthlyLimit} {wallet.Currency}.");
            }

            // Vérifier les limites de l'offre si applicable
            if (wallet.OfferId.HasValue)
            {
                var offer = await _offerRepository.GetByIdAsync(wallet.OfferId.Value);
                if (offer != null && offer.IsActive && offer.ValidTo >= DateTime.UtcNow)
                {
                    if (amount > offer.SpendingLimit)
                    {
                        throw new OfferLimitExceededException($"Cette transaction dépasse la limite de dépense de l'offre ({offer.SpendingLimit} {wallet.Currency}).");
                    }
                }
            }

            // Tout est en ordre
            return true;
        }

        // Méthodes privées
        private bool IsValidMoroccanPhoneNumber(string phoneNumber)
        {
            // Format attendu: +2126XXXXXXXX (Maroc)
            return !string.IsNullOrEmpty(phoneNumber) && 
                   System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\+2126\d{8}$");
        }

        private bool IsValidCinFormat(string cinNumber)
        {
            // Format CIN marocain typique: une ou deux lettres suivies de 5 ou 6 chiffres
            return !string.IsNullOrEmpty(cinNumber) && 
                   System.Text.RegularExpressions.Regex.IsMatch(cinNumber, @"^[A-Za-z]{1,2}\d{5,6}$");
        }

        private async Task<bool> IsUserAdminAsync(string userId)
        {
            // Cette méthode vérifierait dans le système d'authentification si l'utilisateur est un admin
            // Dans un scénario réel, cela dépendrait de votre système d'authentification
            // Pour l'exemple, on suppose l'existence d'une méthode dans un repository
            
            // Mock pour l'exemple
            return await Task.FromResult(false);
        }

        private async Task CheckAndResetDailyLimitsAsync(Entities.Wallet wallet)
        {
            // Réinitialiser l'usage quotidien si on est sur un nouveau jour
            var lastTransaction = await _walletRepository.GetLastTransactionDateAsync(wallet.Id);
            if (lastTransaction.HasValue && lastTransaction.Value.Date < DateTime.UtcNow.Date)
            {
                wallet.CurrentDailyUsage = 0;
                await _walletRepository.UpdateAsync(wallet);
                await _walletRepository.SaveChangesAsync();
            }

            // Réinitialiser l'usage mensuel si on est dans un nouveau mois
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            if (lastTransaction.HasValue && lastTransaction.Value < firstDayOfMonth)
            {
                wallet.CurrentMonthlyUsage = 0;
                await _walletRepository.UpdateAsync(wallet);
                await _walletRepository.SaveChangesAsync();
            }
        }
}

