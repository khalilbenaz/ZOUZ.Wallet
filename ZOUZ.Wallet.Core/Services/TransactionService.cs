using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Exceptions;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Core.Services;

public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IWalletService _walletService;
        private readonly IOfferRepository _offerRepository;
        private readonly IFraudDetectionService _fraudDetectionService;
        private readonly INotificationService _notificationService;
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly IBillPaymentService _billPaymentService;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IWalletRepository walletRepository,
            IWalletService walletService,
            IOfferRepository offerRepository,
            IFraudDetectionService fraudDetectionService,
            INotificationService notificationService,
            IPaymentGatewayService paymentGatewayService,
            IBillPaymentService billPaymentService,
            ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _walletRepository = walletRepository;
            _walletService = walletService;
            _offerRepository = offerRepository;
            _fraudDetectionService = fraudDetectionService;
            _notificationService = notificationService;
            _paymentGatewayService = paymentGatewayService;
            _billPaymentService = billPaymentService;
            _logger = logger;
        }

        public async Task<TransactionResponse> DepositAsync(Guid walletId, DepositRequest request, string userId)
        {
            _logger.LogInformation("Initiating deposit of {Amount} to wallet {WalletId}", request.Amount, walletId);

            // Vérifier que le montant est positif
            if (request.Amount <= 0)
            {
                throw new BusinessRuleException("Le montant du dépôt doit être positif.");
            }

            // Récupérer le wallet
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {walletId} n'existe pas.");
            }

            // Vérifier que l'utilisateur est autorisé à effectuer cette opération
            if (wallet.OwnerId != userId)
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à effectuer des dépôts sur ce wallet.");
            }

            // Vérifier que le wallet est actif
            if (wallet.Status != WalletStatus.Active)
            {
                throw new BusinessRuleException($"Impossible d'effectuer un dépôt sur un wallet {wallet.Status}.");
            }

            // Calculer les frais en fonction de la méthode de paiement et de l'offre
            decimal fee = CalculateDepositFee(request.Amount, request.PaymentMethod, wallet.OfferId);

            // Calculer le montant réel à ajouter
            decimal netAmount = request.Amount - fee;

            // Calculer les bonus éventuels (si l'offre prévoit un bonus de recharge)
            decimal bonus = await CalculateDepositBonusAsync(request.Amount, wallet.OfferId);
            decimal totalAmount = netAmount + bonus;

            // Vérifier si le dépôt est suspect (anti-fraude)
            bool isSuspicious = await _fraudDetectionService.IsSuspiciousDepositAsync(
                walletId, request.Amount, request.PaymentMethod);

            if (isSuspicious)
            {
                _logger.LogWarning("Suspicious deposit detected for wallet {WalletId}", walletId);
                await _notificationService.SendAlertToAdminsAsync(
                    $"Dépôt suspect détecté: {request.Amount} {wallet.Currency} sur le wallet {walletId} " +
                    $"via {request.PaymentMethod}.");
                
                // Selon les règles métier, on pourrait bloquer la transaction ou la mettre en attente
                // Pour cet exemple, nous continuons mais avec une notification
            }

            // Traiter le paiement via la passerelle appropriée
            string paymentReference = string.Empty;
            bool paymentSuccess = false;

            try
            {
                // Le traitement dépend de la méthode de paiement
                switch (request.PaymentMethod)
                {
                    case PaymentMethod.CreditCard:
                        if (string.IsNullOrEmpty(request.CardNumber) || 
                            string.IsNullOrEmpty(request.CardHolderName) ||
                            string.IsNullOrEmpty(request.ExpiryDate) || 
                            string.IsNullOrEmpty(request.Cvv))
                        {
                            throw new ValidationException("Les informations de carte bancaire sont incomplètes.");
                        }
                        
                        paymentReference = await _paymentGatewayService.ProcessCardPaymentAsync(
                            request.CardNumber,
                            request.CardHolderName,
                            request.ExpiryDate,
                            request.Cvv,
                            request.Amount,
                            wallet.Currency.ToString());
                        break;
                        
                    case PaymentMethod.BankTransfer:
                        // Ici, le paiement serait déjà effectué et on enregistrerait juste la transaction
                        paymentReference = Guid.NewGuid().ToString();
                        break;
                        
                    case PaymentMethod.OrangeMoney:
                    case PaymentMethod.InwiMoney:
                        if (string.IsNullOrEmpty(request.MobileOperatorReference))
                        {
                            throw new ValidationException("La référence de l'opérateur mobile est requise.");
                        }
                        
                        paymentReference = await _paymentGatewayService.VerifyMobilePaymentAsync(
                            request.MobileOperatorReference,
                            request.PaymentMethod.ToString(),
                            request.Amount);
                        break;
                        
                    default:
                        throw new BusinessRuleException($"Méthode de paiement {request.PaymentMethod} non supportée.");
                }

                paymentSuccess = !string.IsNullOrEmpty(paymentReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed for wallet {WalletId}", walletId);
                paymentSuccess = false;
            }

            // Créer la transaction
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                Type = TransactionType.Deposit,
                Amount = request.Amount,
                Fee = fee,
                Description = request.Description ?? $"Dépôt via {request.PaymentMethod}",
                ReferenceNumber = paymentReference,
                IsSuccessful = paymentSuccess,
                FailureReason = !paymentSuccess ? "Échec du traitement du paiement" : null,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(transaction);

            // Si le paiement a réussi, mettre à jour le solde du wallet
            if (paymentSuccess)
            {
                wallet.Balance += totalAmount;
                wallet.UpdatedAt = DateTime.UtcNow;
                
                // Mettre à jour les limites d'utilisation
                // Généralement, les dépôts ne comptent pas dans les limites de dépense
                // mais on pourrait avoir des règles spécifiques
                
                await _walletRepository.UpdateAsync(wallet);

                // Si un bonus a été appliqué, créer une transaction distincte pour le bonus
                if (bonus > 0)
                {
                    var bonusTransaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = walletId,
                        Type = TransactionType.Bonus,
                        Amount = bonus,
                        Fee = 0,
                        Description = $"Bonus de recharge ({bonus} {wallet.Currency})",
                        ReferenceNumber = transaction.Id.ToString(),
                        IsSuccessful = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _transactionRepository.AddAsync(bonusTransaction);
                }

                // Envoyer une notification au client
                await _notificationService.SendTransactionNotificationAsync(
                    wallet.OwnerId,
                    $"Dépôt de {request.Amount} {wallet.Currency} réussi. Nouveau solde: {wallet.Balance} {wallet.Currency}");
            }

            await _transactionRepository.SaveChangesAsync();

            // Construire la réponse
            var response = new TransactionResponse
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Fee = transaction.Fee,
                Description = transaction.Description,
                ReferenceNumber = transaction.ReferenceNumber,
                IsSuccessful = transaction.IsSuccessful,
                FailureReason = transaction.FailureReason,
                PaymentMethod = transaction.PaymentMethod,
                CreatedAt = transaction.CreatedAt
            };

            return response;
        }

        public async Task<TransactionResponse> WithdrawAsync(Guid walletId, WithdrawalRequest request, string userId)
        {
            _logger.LogInformation("Initiating withdrawal of {Amount} from wallet {WalletId}", request.Amount, walletId);

            // Vérifier que le montant est positif
            if (request.Amount <= 0)
            {
                throw new BusinessRuleException("Le montant du retrait doit être positif.");
            }

            // Récupérer le wallet
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {walletId} n'existe pas.");
            }

            // Vérifier que l'utilisateur est autorisé à effectuer cette opération
            if (wallet.OwnerId != userId)
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à effectuer des retraits depuis ce wallet.");
            }

            // Vérifier que le wallet est actif
            if (wallet.Status != WalletStatus.Active)
            {
                throw new BusinessRuleException($"Impossible d'effectuer un retrait depuis un wallet {wallet.Status}.");
            }

            // Calculer les frais en fonction de la méthode de paiement et de l'offre
            decimal fee = CalculateWithdrawalFee(request.Amount, request.PaymentMethod, wallet.OfferId);

            // Calculer le montant total à déduire
            decimal totalAmount = request.Amount + fee;

            // Vérifier si le wallet peut effectuer cette transaction
            await _walletService.CanPerformTransactionAsync(walletId, totalAmount, userId);

            // Vérifier si le retrait est suspect (anti-fraude)
            bool isSuspicious = await _fraudDetectionService.IsSuspiciousWithdrawalAsync(
                walletId, request.Amount, request.PaymentMethod);

            if (isSuspicious)
            {
                _logger.LogWarning("Suspicious withdrawal detected for wallet {WalletId}", walletId);
                await _notificationService.SendAlertToAdminsAsync(
                    $"Retrait suspect détecté: {request.Amount} {wallet.Currency} depuis le wallet {walletId} " +
                    $"via {request.PaymentMethod}.");
            }

            // Traiter le retrait via la passerelle appropriée
            string paymentReference = string.Empty;
            bool paymentSuccess = false;

            try
            {
                // Le traitement dépend de la méthode de paiement
                switch (request.PaymentMethod)
                {
                    case PaymentMethod.BankTransfer:
                        if (string.IsNullOrEmpty(request.BankAccountNumber) || 
                            string.IsNullOrEmpty(request.BankName))
                        {
                            throw new ValidationException("Les informations bancaires sont incomplètes.");
                        }
                        
                        paymentReference = await _paymentGatewayService.ProcessBankWithdrawalAsync(
                            request.BankAccountNumber,
                            request.BankName,
                            request.Amount,
                            wallet.Currency.ToString());
                        break;
                        
                    case PaymentMethod.OrangeMoney:
                    case PaymentMethod.InwiMoney:
                        if (string.IsNullOrEmpty(request.RecipientPhoneNumber))
                        {
                            throw new ValidationException("Le numéro de téléphone du destinataire est requis.");
                        }
                        
                        paymentReference = await _paymentGatewayService.ProcessMobileWithdrawalAsync(
                            request.RecipientPhoneNumber,
                            request.PaymentMethod.ToString(),
                            request.Amount);
                        break;
                        
                    case PaymentMethod.Cash:
                        // Retrait en espèces via un agent partenaire
                        // Dans un scénario réel, cela générerait un code que l'utilisateur présenterait à l'agent
                        paymentReference = Guid.NewGuid().ToString();
                        break;
                        
                    default:
                        throw new BusinessRuleException($"Méthode de retrait {request.PaymentMethod} non supportée.");
                }

                paymentSuccess = !string.IsNullOrEmpty(paymentReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Withdrawal processing failed for wallet {WalletId}", walletId);
                paymentSuccess = false;
            }

            // Créer la transaction
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                Type = TransactionType.Withdrawal,
                Amount = request.Amount,
                Fee = fee,
                Description = request.Description ?? $"Retrait via {request.PaymentMethod}",
                ReferenceNumber = paymentReference,
                IsSuccessful = paymentSuccess,
                FailureReason = !paymentSuccess ? "Échec du traitement du retrait" : null,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(transaction);

            // Si le retrait a réussi, mettre à jour le solde du wallet
            if (paymentSuccess)
            {
                wallet.Balance -= totalAmount;
                wallet.UpdatedAt = DateTime.UtcNow;
                
                // Mettre à jour les limites d'utilisation
                wallet.CurrentDailyUsage += totalAmount;
                wallet.CurrentMonthlyUsage += totalAmount;
                
                await _walletRepository.UpdateAsync(wallet);

                // Créer une transaction distincte pour les frais
                if (fee > 0)
                {
                    var feeTransaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = walletId,
                        Type = TransactionType.Fee,
                        Amount = fee,
                        Fee = 0,
                        Description = $"Frais de retrait ({fee} {wallet.Currency})",
                        ReferenceNumber = transaction.Id.ToString(),
                        IsSuccessful = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _transactionRepository.AddAsync(feeTransaction);
                }

                // Envoyer une notification au client
                await _notificationService.SendTransactionNotificationAsync(
                    wallet.OwnerId,
                    $"Retrait de {request.Amount} {wallet.Currency} réussi. Nouveau solde: {wallet.Balance} {wallet.Currency}");
            }

            await _transactionRepository.SaveChangesAsync();

            // Construire la réponse
            var response = new TransactionResponse
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Fee = transaction.Fee,
                Description = transaction.Description,
                ReferenceNumber = transaction.ReferenceNumber,
                IsSuccessful = transaction.IsSuccessful,
                FailureReason = transaction.FailureReason,
                PaymentMethod = transaction.PaymentMethod,
                CreatedAt = transaction.CreatedAt
            };

            return response;
        }

        public async Task<TransactionResponse> TransferAsync(TransferRequest request, string userId)
        {
            _logger.LogInformation("Initiating transfer of {Amount} from wallet {SourceWalletId} to {DestinationWalletId}", 
                request.Amount, request.SourceWalletId, request.DestinationWalletId);

            // Vérifier que le montant est positif
            if (request.Amount <= 0)
            {
                throw new BusinessRuleException("Le montant du transfert doit être positif.");
            }

            // Vérifier que les wallets sont différents
            if (request.SourceWalletId == request.DestinationWalletId)
            {
                throw new BusinessRuleException("Impossible de transférer vers le même wallet.");
            }

            // Récupérer les wallets
            var sourceWallet = await _walletRepository.GetByIdAsync(request.SourceWalletId);
            if (sourceWallet == null)
            {
                throw new NotFoundException($"Le wallet source avec l'ID {request.SourceWalletId} n'existe pas.");
            }

            var destinationWallet = await _walletRepository.GetByIdAsync(request.DestinationWalletId);
            if (destinationWallet == null)
            {
                throw new NotFoundException($"Le wallet destination avec l'ID {request.DestinationWalletId} n'existe pas.");
            }

            // Vérifier que l'utilisateur est autorisé à effectuer cette opération
            if (sourceWallet.OwnerId != userId)
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à effectuer des transferts depuis ce wallet.");
            }

            // Vérifier que les wallets sont actifs
            if (sourceWallet.Status != WalletStatus.Active)
            {
                throw new BusinessRuleException($"Impossible d'effectuer un transfert depuis un wallet {sourceWallet.Status}.");
            }

            if (destinationWallet.Status != WalletStatus.Active)
            {
                throw new BusinessRuleException($"Impossible d'effectuer un transfert vers un wallet {destinationWallet.Status}.");
            }

            // Vérifier si les devises sont compatibles
            if (sourceWallet.Currency != destinationWallet.Currency)
            {
                throw new BusinessRuleException("Les transferts entre devises différentes ne sont pas encore supportés.");
            }

            // Calculer les frais en fonction de l'offre et du montant
            decimal fee = CalculateTransferFee(request.Amount, sourceWallet.OfferId);

            // Calculer le montant total à déduire
            decimal totalDebit = request.Amount + fee;

            // Vérifier si le wallet source peut effectuer cette transaction
            await _walletService.CanPerformTransactionAsync(request.SourceWalletId, totalDebit, userId);

            // Vérifier pour 2FA si le montant dépasse un certain seuil
            if (request.Amount > 1000 && string.IsNullOrEmpty(request.OtpCode))
            {
                throw new BusinessRuleException("Un code d'authentification à deux facteurs est requis pour les transferts supérieurs à 1000 MAD.");
            }

            // Vérifier le code OTP si fourni
            if (!string.IsNullOrEmpty(request.OtpCode))
            {
                bool otpValid = await VerifyOtpAsync(sourceWallet.OwnerId, request.OtpCode);
                if (!otpValid)
                {
                    throw new BusinessRuleException("Le code d'authentification à deux facteurs est invalide ou a expiré.");
                }
            }

            // Vérifier si le transfert est suspect (anti-fraude)
            bool isSuspicious = await _fraudDetectionService.IsSuspiciousTransferAsync(
                request.SourceWalletId, request.DestinationWalletId, request.Amount);

            if (isSuspicious)
            {
                _logger.LogWarning("Suspicious transfer detected from {SourceWalletId} to {DestinationWalletId}", 
                    request.SourceWalletId, request.DestinationWalletId);
                
                await _notificationService.SendAlertToAdminsAsync(
                    $"Transfert suspect détecté: {request.Amount} {sourceWallet.Currency} " +
                    $"de {request.SourceWalletId} vers {request.DestinationWalletId}.");
            }

            // Créer la transaction
            var referenceNumber = Guid.NewGuid().ToString();
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = request.SourceWalletId,
                DestinationWalletId = request.DestinationWalletId,
                Type = TransactionType.Transfer,
                Amount = request.Amount,
                Fee = fee,
                Description = request.Description ?? "Transfert entre wallets",
                ReferenceNumber = referenceNumber,
                IsSuccessful = true,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(transaction);

            // Mettre à jour les soldes des wallets
            sourceWallet.Balance -= totalDebit;
            sourceWallet.UpdatedAt = DateTime.UtcNow;
            sourceWallet.CurrentDailyUsage += totalDebit;
            sourceWallet.CurrentMonthlyUsage += totalDebit;
            
            destinationWallet.Balance += request.Amount;
            destinationWallet.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.UpdateAsync(sourceWallet);
            await _walletRepository.UpdateAsync(destinationWallet);

            // Créer une transaction distincte pour les frais
            if (fee > 0)
            {
                var feeTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = request.SourceWalletId,
                    Type = TransactionType.Fee,
                    Amount = fee,
                    Fee = 0,
                    Description = $"Frais de transfert ({fee} {sourceWallet.Currency})",
                    ReferenceNumber = transaction.Id.ToString(),
                    IsSuccessful = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _transactionRepository.AddAsync(feeTransaction);
            }

            // Envoyer des notifications aux deux clients
            await _notificationService.SendTransactionNotificationAsync(
                sourceWallet.OwnerId,
                $"Transfert de {request.Amount} {sourceWallet.Currency} effectué. Nouveau solde: {sourceWallet.Balance} {sourceWallet.Currency}");
            
            await _notificationService.SendTransactionNotificationAsync(
                destinationWallet.OwnerId,
                $"Transfert de {request.Amount} {destinationWallet.Currency} reçu de {sourceWallet.OwnerName}. Nouveau solde: {destinationWallet.Balance} {destinationWallet.Currency}");

            await _transactionRepository.SaveChangesAsync();

            // Construire la réponse
            var response = new TransactionResponse
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                DestinationWalletId = transaction.DestinationWalletId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Fee = transaction.Fee,
                Description = transaction.Description,
                ReferenceNumber = transaction.ReferenceNumber,
                IsSuccessful = transaction.IsSuccessful,
                CreatedAt = transaction.CreatedAt
            };

            return response;
        }

        public async Task<TransactionResponse> PayBillAsync(Guid walletId, PayBillRequest request, string userId)
        {
            _logger.LogInformation("Initiating bill payment of {Amount} from wallet {WalletId}", request.Amount, walletId);

            // Vérifier que le montant est positif
            if (request.Amount <= 0)
            {
                throw new BusinessRuleException("Le montant du paiement doit être positif.");
            }

            // Récupérer le wallet
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {walletId} n'existe pas.");
            }

            // Vérifier que l'utilisateur est autorisé à effectuer cette opération
            if (wallet.OwnerId != userId)
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à effectuer des paiements depuis ce wallet.");
            }

            // Vérifier que le wallet est actif
            if (wallet.Status != WalletStatus.Active)
            {
                throw new BusinessRuleException($"Impossible d'effectuer un paiement depuis un wallet {wallet.Status}.");
            }

            // Calculer les frais en fonction du type de facture et de l'offre
            decimal fee = CalculateBillPaymentFee(request.Amount, request.BillType, wallet.OfferId);

            // Calculer le montant total à déduire
            decimal totalAmount = request.Amount + fee;

            // Vérifier si le wallet peut effectuer cette transaction
            await _walletService.CanPerformTransactionAsync(walletId, totalAmount, userId);

            // Vérifier l'existence de la facture auprès du fournisseur
            var billVerificationResult = await _billPaymentService.VerifyBillAsync(
                request.BillerName,
                request.BillerReference,
                request.CustomerReference,
                request.Amount);

            if (!billVerificationResult.IsValid)
            {
                throw new BusinessRuleException($"Vérification de la facture échouée: {billVerificationResult.Message}");
            }

            // Créer l'entrée de facture
            var bill = new Bill
            {
                Id = Guid.NewGuid(),
                BillerName = request.BillerName,
                BillerReference = request.BillerReference,
                CustomerReference = request.CustomerReference,
                Amount = request.Amount,
                DueDate = billVerificationResult.DueDate,
                IsPaid = false,
                BillType = request.BillType,
                CreatedAt = DateTime.UtcNow
            };

            // Effectuer le paiement auprès du fournisseur
            string paymentReference = string.Empty;
            bool paymentSuccess = false;

            try
            {
                paymentReference = await _billPaymentService.PayBillAsync(
                    request.BillerName,
                    request.BillerReference,
                    request.CustomerReference,
                    request.Amount,
                    request.BillType);

                paymentSuccess = !string.IsNullOrEmpty(paymentReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bill payment processing failed for wallet {WalletId}", walletId);
                paymentSuccess = false;
            }

            // Créer la transaction
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                Type = TransactionType.BillPayment,
                Amount = request.Amount,
                Fee = fee,
                Description = $"Paiement {request.BillType} - {request.BillerName}",
                ReferenceNumber = paymentReference,
                IsSuccessful = paymentSuccess,
                FailureReason = !paymentSuccess ? "Échec du traitement du paiement de facture" : null,
                BillId = bill.Id,
                CreatedAt = DateTime.UtcNow
            };

            // Mettre à jour la facture
            if (paymentSuccess)
            {
                bill.IsPaid = true;
                bill.PaymentDate = DateTime.UtcNow;
            }

            await _transactionRepository.AddBillAsync(bill);
            await _transactionRepository.AddAsync(transaction);

            // Si le paiement a réussi, mettre à jour le solde du wallet
            if (paymentSuccess)
            {
                wallet.Balance -= totalAmount;
                wallet.UpdatedAt = DateTime.UtcNow;
                
                // Mettre à jour les limites d'utilisation
                wallet.CurrentDailyUsage += totalAmount;
                wallet.CurrentMonthlyUsage += totalAmount;
                
                await _walletRepository.UpdateAsync(wallet);

                // Créer une transaction distincte pour les frais
                if (fee > 0)
                {
                    var feeTransaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = walletId,
                        Type = TransactionType.Fee,
                        Amount = fee,
                        Fee = 0,
                        Description = $"Frais de paiement de facture ({fee} {wallet.Currency})",
                        ReferenceNumber = transaction.Id.ToString(),
                        IsSuccessful = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _transactionRepository.AddAsync(feeTransaction);
                }

                // Envoyer une notification au client
                await _notificationService.SendTransactionNotificationAsync(
                    wallet.OwnerId,
                    $"Paiement de facture {request.BillerName} de {request.Amount} {wallet.Currency} réussi. " +
                    $"Nouveau solde: {wallet.Balance} {wallet.Currency}");
            }

            await _transactionRepository.SaveChangesAsync();

            // Construire la réponse
            var response = new TransactionResponse
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Fee = transaction.Fee,
                Description = transaction.Description,
                ReferenceNumber = transaction.ReferenceNumber,
                IsSuccessful = transaction.IsSuccessful,
                FailureReason = transaction.FailureReason,
                CreatedAt = transaction.CreatedAt,
                Bill = new BillResponse
                {
                    Id = bill.Id,
                    BillerName = bill.BillerName,
                    BillerReference = bill.BillerReference,
                    CustomerReference = bill.CustomerReference,
                    Amount = bill.Amount,
                    DueDate = bill.DueDate,
                    IsPaid = bill.IsPaid,
                    PaymentDate = bill.PaymentDate,
                    BillType = bill.BillType
                }
            };

            return response;
        }

        public async Task<PagedResponse<TransactionResponse>> GetWalletTransactionsAsync(
            Guid walletId, 
            DateTime? startDate, 
            DateTime? endDate, 
            TransactionType? type,
            int pageNumber, 
            int pageSize, 
            string userId)
        {
            // Récupérer le wallet
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                throw new NotFoundException($"Le wallet avec l'ID {walletId} n'existe pas.");
            }

            // Vérifier que l'utilisateur est autorisé à accéder à ces informations
            if (wallet.OwnerId != userId && !await IsUserAdminAsync(userId))
            {
                throw new UnauthorizedException("Vous n'êtes pas autorisé à accéder aux transactions de ce wallet.");
            }

            // Récupérer les transactions
            var transactions = await _transactionRepository.GetTransactionsAsync(
                walletId,
                startDate,
                endDate,
                type,
                pageNumber,
                pageSize);

            // Récupérer le nombre total de transactions pour la pagination
            var totalCount = await _transactionRepository.CountTransactionsAsync(
                walletId,
                startDate,
                endDate,
                type);

            // Mapper vers les réponses
            var transactionResponses = transactions.Select(t => new TransactionResponse
            {
                Id = t.Id,
                WalletId = t.WalletId,
                DestinationWalletId = t.DestinationWalletId,
                Type = t.Type,
                Amount = t.Amount,
                Fee = t.Fee,
                Cashback = t.Cashback,
                Description = t.Description,
                ReferenceNumber = t.ReferenceNumber,
                IsSuccessful = t.IsSuccessful,
                FailureReason = t.FailureReason,
                PaymentMethod = t.PaymentMethod,
                CreatedAt = t.CreatedAt,
                Bill = t.Bill != null ? new BillResponse
                {
                    Id = t.Bill.Id,
                    BillerName = t.Bill.BillerName,
                    BillerReference = t.Bill.BillerReference,
                    CustomerReference = t.Bill.CustomerReference,
                    Amount = t.Bill.Amount,
                    DueDate = t.Bill.DueDate,
                    IsPaid = t.Bill.IsPaid,
                    PaymentDate = t.Bill.PaymentDate,
                    BillType = t.Bill.BillType
                } : null
            }).ToList();

            return new PagedResponse<TransactionResponse>(
                transactionResponses,
                pageNumber,
                pageSize,
                totalCount);
        }

        // Méthodes privées pour le calcul des frais
        private decimal CalculateDepositFee(decimal amount, PaymentMethod method, Guid? offerId)
        {
            // Taux de base en fonction de la méthode de paiement
            decimal baseFeeRate = method switch
            {
                PaymentMethod.CreditCard => 0.015m, // 1.5%
                PaymentMethod.BankTransfer => 0.005m, // 0.5%
                PaymentMethod.OrangeMoney => 0.01m, // 1%
                PaymentMethod.InwiMoney => 0.01m, // 1%
                _ => 0.02m // 2% par défaut
            };

            // Calculer le frais de base
            decimal baseFee = amount * baseFeeRate;

            // Appliquer une réduction de frais si l'utilisateur a une offre
            if (offerId.HasValue)
            {
                var offer = _offerRepository.GetByIdAsync(offerId.Value).Result;
                if (offer != null && offer.IsActive && offer.ValidTo >= DateTime.UtcNow)
                {
                    if (offer.FeesDiscount.HasValue)
                    {
                        baseFee -= baseFee * (offer.FeesDiscount.Value / 100);
                    }
                }
            }

            // Arrondir à deux décimales
            return Math.Round(baseFee, 2);
        }

        private decimal CalculateWithdrawalFee(decimal amount, PaymentMethod method, Guid? offerId)
        {
            // Taux de base en fonction de la méthode de retrait
            decimal baseFeeRate = method switch
            {
                PaymentMethod.BankTransfer => 0.01m, // 1%
                PaymentMethod.OrangeMoney => 0.015m, // 1.5%
                PaymentMethod.InwiMoney => 0.015m, // 1.5%
                PaymentMethod.Cash => 0.02m, // 2%
                _ => 0.02m // 2% par défaut
            };

            // Calculer le frais de base
            decimal baseFee = amount * baseFeeRate;

            // Appliquer une réduction de frais si l'utilisateur a une offre
            if (offerId.HasValue)
            {
                var offer = _offerRepository.GetByIdAsync(offerId.Value).Result;
                if (offer != null && offer.IsActive && offer.ValidTo >= DateTime.UtcNow)
                {
                    if (offer.FeesDiscount.HasValue)
                    {
                        baseFee -= baseFee * (offer.FeesDiscount.Value / 100);
                    }
                }
            }

            // Arrondir à deux décimales
            return Math.Round(baseFee, 2);
        }

        private decimal CalculateTransferFee(decimal amount, Guid? offerId)
        {
            // Taux de base pour les transferts
            decimal baseFeeRate = 0.01m; // 1%

            // Calculer le frais de base
            decimal baseFee = amount * baseFeeRate;

            // Appliquer une réduction de frais si l'utilisateur a une offre
            if (offerId.HasValue)
            {
                var offer = _offerRepository.GetByIdAsync(offerId.Value).Result;
                if (offer != null && offer.IsActive && offer.ValidTo >= DateTime.UtcNow)
                {
                    if (offer.FeesDiscount.HasValue)
                    {
                        baseFee -= baseFee * (offer.FeesDiscount.Value / 100);
                    }
                }
            }

            // Arrondir à deux décimales
            return Math.Round(baseFee, 2);
        }

        private decimal CalculateBillPaymentFee(decimal amount, string billType, Guid? offerId)
        {
            // Taux de base en fonction du type de facture
            decimal baseFeeRate = billType.ToLower() switch
            {
                "telecom" => 0.01m, // 1%
                "eau" => 0.005m, // 0.5%
                "électricité" => 0.005m, // 0.5%
                "taxes" => 0.015m, // 1.5%
                _ => 0.01m // 1% par défaut
            };

            // Calculer le frais de base
            decimal baseFee = amount * baseFeeRate;

            // Appliquer une réduction de frais si l'utilisateur a une offre
            if (offerId.HasValue)
            {
                var offer = _offerRepository.GetByIdAsync(offerId.Value).Result;
                if (offer != null && offer.IsActive && offer.ValidTo >= DateTime.UtcNow)
                {
                    if (offer.FeesDiscount.HasValue)
                    {
                        baseFee -= baseFee * (offer.FeesDiscount.Value / 100);
                    }
                }
            }

            // Arrondir à deux décimales
            return Math.Round(baseFee, 2);
        }

        private async Task<decimal> CalculateDepositBonusAsync(decimal amount, Guid? offerId)
        {
            // Pas de bonus par défaut
            decimal bonus = 0;

            // Appliquer un bonus si l'utilisateur a une offre
            if (offerId.HasValue)
            {
                var offer = await _offerRepository.GetByIdAsync(offerId.Value);
                if (offer != null && offer.IsActive && offer.ValidTo >= DateTime.UtcNow)
                {
                    if (offer.Type == OfferType.RechargeBonus && offer.RechargeBonus.HasValue)
                    {
                        bonus = amount * (offer.RechargeBonus.Value / 100);
                    }
                }
            }

            // Arrondir à deux décimales
            return Math.Round(bonus, 2);
        }

        private async Task<bool> VerifyOtpAsync(string userId, string otpCode)
        {
            // Dans un scénario réel, vérifier le code OTP dans un service dédié
            // Pour l'exemple, nous faisons une simple vérification factice
            return await Task.FromResult(otpCode == "123456");
        }

        private async Task<bool> IsUserAdminAsync(string userId)
        {
            // Cette méthode vérifierait dans le système d'authentification si l'utilisateur est un admin
            // Pour l'exemple, nous faisons une simple vérification factice
            return await Task.FromResult(false);
        }
    }