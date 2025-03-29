using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ZOUZ.Wallet.Core.DTOs.Base;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Exceptions;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Core.Services;

public class KycService : IKycService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly INotificationService _notificationService;
        private readonly ILogger<KycService> _logger;

        public KycService(
            IWalletRepository walletRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            INotificationService notificationService,
            ILogger<KycService> logger)
        {
            _walletRepository = walletRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<bool> InitiateBasicVerificationAsync(Guid walletId, string cinNumber)
        {
            _logger.LogInformation("Initiating basic KYC verification for wallet {WalletId} with CIN {CinNumber}", walletId, cinNumber);

            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                _logger.LogError("Wallet {WalletId} not found for KYC verification", walletId);
                throw new NotFoundException($"Wallet with ID {walletId} does not exist.");
            }

            // Dans un environnement réel, nous appellerions un service externe de vérification KYC
            // Pour l'exemple, nous faisons une vérification locale simplifiée

            // Vérification basique du format CIN marocain (une ou deux lettres suivies de 5 ou 6 chiffres)
            bool isValidFormat = !string.IsNullOrEmpty(cinNumber) && 
                                System.Text.RegularExpressions.Regex.IsMatch(cinNumber, @"^[A-Za-z]{1,2}\d{5,6}$");

            if (!isValidFormat)
            {
                _logger.LogWarning("Invalid CIN format {CinNumber} for wallet {WalletId}", cinNumber, walletId);
                return false;
            }

            // Mettre à jour le niveau KYC et enregistrer le CIN
            wallet.CinNumber = cinNumber;
            wallet.KycLevel = KycLevel.Basic;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.UpdateAsync(wallet);
            await _walletRepository.SaveChangesAsync();

            // Envoyer une notification à l'utilisateur
            await _notificationService.SendTransactionNotificationAsync(
                wallet.OwnerId,
                "Votre vérification d'identité de base a été effectuée avec succès. Vous pouvez maintenant bénéficier de limites de transaction plus élevées.");

            _logger.LogInformation("Basic KYC verification completed for wallet {WalletId}", walletId);
            return true;
        }

        public async Task<bool> VerifyIdentityAsync(Guid walletId, VerifyIdentityRequest request)
        {
            _logger.LogInformation("Processing full identity verification for wallet {WalletId}", walletId);

            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                _logger.LogError("Wallet {WalletId} not found for identity verification", walletId);
                throw new NotFoundException($"Wallet with ID {walletId} does not exist.");
            }

            try
            {
                // Dans un environnement réel, nous appellerions un service externe comme Bank Al-Maghrib ou un fournisseur KYC
                // Pour l'exemple, nous simulons cet appel

                var apiKey = _configuration["KycService:ApiKey"];
                var apiUrl = _configuration["KycService:ApiUrl"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
                {
                    _logger.LogError("KYC service configuration is missing");
                    throw new Exception("KYC service configuration is missing");
                }

                // Simuler la vérification
                bool verificationSuccess = !string.IsNullOrEmpty(request.CinNumber) && 
                                         !string.IsNullOrEmpty(request.FullName) &&
                                         request.DateOfBirth < DateTime.UtcNow.AddYears(-18) && // Vérifier l'âge (18+ ans)
                                         !string.IsNullOrEmpty(request.CinFrontImage) &&
                                         !string.IsNullOrEmpty(request.CinBackImage) &&
                                         !string.IsNullOrEmpty(request.SelfieImage);

                if (verificationSuccess)
                {
                    // Mettre à jour le niveau KYC en fonction des documents fournis
                    wallet.CinNumber = request.CinNumber;
                    wallet.IsIdentityVerified = true;
                    wallet.VerificationDate = DateTime.UtcNow;
                    wallet.KycLevel = KycLevel.Standard; // Niveau standard pour une vérification CIN complète
                    wallet.UpdatedAt = DateTime.UtcNow;

                    // Mettre à jour les limites en fonction du nouveau niveau KYC
                    wallet.DailyLimit = 10000m; // 10000 MAD pour le niveau standard
                    wallet.MonthlyLimit = 50000m; // 50000 MAD pour le niveau standard

                    await _walletRepository.UpdateAsync(wallet);
                    await _walletRepository.SaveChangesAsync();

                    // Envoyer une notification à l'utilisateur
                    await _notificationService.SendTransactionNotificationAsync(
                        wallet.OwnerId,
                        "Votre vérification d'identité a été effectuée avec succès. Vous bénéficiez maintenant de limites de transaction plus élevées.");

                    _logger.LogInformation("Identity verification successful for wallet {WalletId}", walletId);
                    return true;
                }

                _logger.LogWarning("Identity verification failed for wallet {WalletId}", walletId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during identity verification for wallet {WalletId}", walletId);
                throw;
            }
        }

        public async Task<bool> UpgradeKycLevelAsync(Guid walletId, KycLevel newLevel)
        {
            _logger.LogInformation("Upgrading KYC level for wallet {WalletId} to {NewLevel}", walletId, newLevel);

            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                _logger.LogError("Wallet {WalletId} not found for KYC level upgrade", walletId);
                throw new NotFoundException($"Wallet with ID {walletId} does not exist.");
            }

            // Vérifier que c'est une mise à niveau (et non une rétrogradation)
            if ((int)newLevel <= (int)wallet.KycLevel)
            {
                _logger.LogWarning("Attempted to downgrade KYC level from {CurrentLevel} to {NewLevel} for wallet {WalletId}", 
                    wallet.KycLevel, newLevel, walletId);
                throw new BusinessRuleException("Cannot downgrade KYC level.");
            }

            // Vérifier les prérequis pour le nouveau niveau
            if (newLevel == KycLevel.Advanced && !wallet.IsIdentityVerified)
            {
                _logger.LogWarning("Attempted to upgrade to Advanced KYC without identity verification for wallet {WalletId}", walletId);
                throw new BusinessRuleException("Identity verification is required before upgrading to Advanced KYC level.");
            }

            // Mettre à jour le niveau KYC et les limites
            wallet.KycLevel = newLevel;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Mettre à jour les limites en fonction du nouveau niveau KYC
            switch (newLevel)
            {
                case KycLevel.Standard:
                    wallet.DailyLimit = 10000m;
                    wallet.MonthlyLimit = 50000m;
                    break;
                case KycLevel.Advanced:
                    wallet.DailyLimit = 20000m;
                    wallet.MonthlyLimit = 100000m;
                    break;
            }

            await _walletRepository.UpdateAsync(wallet);
            await _walletRepository.SaveChangesAsync();

            // Envoyer une notification à l'utilisateur
            await _notificationService.SendTransactionNotificationAsync(
                wallet.OwnerId,
                $"Votre niveau de vérification a été mis à jour vers {newLevel}. Vous bénéficiez maintenant de limites de transaction plus élevées.");

            _logger.LogInformation("KYC level upgraded to {NewLevel} for wallet {WalletId}", newLevel, walletId);
            return true;
        }

        public async Task<KycVerificationStatus> CheckVerificationStatusAsync(Guid walletId)
        {
            _logger.LogInformation("Checking KYC verification status for wallet {WalletId}", walletId);

            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                _logger.LogError("Wallet {WalletId} not found for KYC status check", walletId);
                throw new NotFoundException($"Wallet with ID {walletId} does not exist.");
            }

            var status = new KycVerificationStatus
            {
                IsVerified = wallet.IsIdentityVerified,
                CurrentLevel = wallet.KycLevel,
                VerificationDate = wallet.VerificationDate,
                // La date d'expiration serait à 1 an de la vérification (selon les règles de Bank Al-Maghrib)
                ExpiryDate = wallet.VerificationDate?.AddYears(1)
            };

            switch (wallet.KycLevel)
            {
                case KycLevel.None:
                    status.Message = "Aucune vérification d'identité n'a été effectuée.";
                    break;
                case KycLevel.Basic:
                    status.Message = "Vérification de base effectuée. Vous pouvez améliorer vos limites en complétant la vérification d'identité.";
                    break;
                case KycLevel.Standard:
                    status.Message = "Vérification standard effectuée. Votre identité a été vérifiée avec succès.";
                    break;
                case KycLevel.Advanced:
                    status.Message = "Vérification avancée effectuée. Vous bénéficiez des limites de transaction les plus élevées.";
                    break;
            }

            return status;
        }
    }