using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Infrastructure.Services;

public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentGatewayService> _logger;
        private readonly HttpClient _httpClient;

        public PaymentGatewayService(
            IConfiguration configuration,
            ILogger<PaymentGatewayService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("PaymentGateway");
        }

        public async Task<string> ProcessCardPaymentAsync(string cardNumber, string cardHolderName, string expiryDate, string cvv, decimal amount, string currency)
        {
            try
            {
                _logger.LogInformation("Processing card payment of {Amount} {Currency}", amount, currency);

                // Dans un cas réel, on appellerait l'API de la passerelle de paiement
                // Pour l'exemple, on simule le traitement
                
                // Vérification de base de la carte
                if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 13 || cardNumber.Length > 19)
                {
                    throw new Exception("Numéro de carte invalide");
                }

                if (string.IsNullOrEmpty(expiryDate) || !IsValidExpiryDate(expiryDate))
                {
                    throw new Exception("Date d'expiration invalide");
                }

                if (string.IsNullOrEmpty(cvv) || cvv.Length < 3 || cvv.Length > 4)
                {
                    throw new Exception("CVV invalide");
                }

                // Simuler une réponse réussie
                var transactionReference = Guid.NewGuid().ToString();
                _logger.LogInformation("Card payment processed successfully: {TransactionReference}", transactionReference);
                
                return transactionReference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process card payment");
                throw;
            }
        }

        public async Task<string> VerifyMobilePaymentAsync(string reference, string provider, decimal amount)
        {
            try
            {
                _logger.LogInformation("Verifying mobile payment of {Amount} via {Provider}", amount, provider);

                // Dans un cas réel, on vérifierait auprès du fournisseur de paiement mobile (Orange Money, Inwi Money)
                // Pour l'exemple, on simule la vérification
                
                if (string.IsNullOrEmpty(reference))
                {
                    throw new Exception("Référence de paiement invalide");
                }

                // Simuler une réponse réussie
                var transactionReference = Guid.NewGuid().ToString();
                _logger.LogInformation("Mobile payment verified successfully: {TransactionReference}", transactionReference);
                
                return transactionReference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify mobile payment");
                throw;
            }
        }

        public async Task<string> ProcessBankWithdrawalAsync(string accountNumber, string bankName, decimal amount, string currency)
        {
            try
            {
                _logger.LogInformation("Processing bank withdrawal of {Amount} {Currency} to {BankName}", amount, currency, bankName);

                // Dans un cas réel, on initierait un virement vers le compte bancaire spécifié
                // Pour l'exemple, on simule le traitement
                
                if (string.IsNullOrEmpty(accountNumber) || string.IsNullOrEmpty(bankName))
                {
                    throw new Exception("Informations bancaires incomplètes");
                }

                // Simuler une réponse réussie
                var transactionReference = Guid.NewGuid().ToString();
                _logger.LogInformation("Bank withdrawal processed successfully: {TransactionReference}", transactionReference);
                
                return transactionReference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process bank withdrawal");
                throw;
            }
        }

        public async Task<string> ProcessMobileWithdrawalAsync(string phoneNumber, string provider, decimal amount)
        {
            try
            {
                _logger.LogInformation("Processing mobile withdrawal of {Amount} to {PhoneNumber} via {Provider}", amount, phoneNumber, provider);

                // Dans un cas réel, on initierait un transfert vers le compte mobile spécifié
                // Pour l'exemple, on simule le traitement
                
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    throw new Exception("Numéro de téléphone invalide");
                }

                // Simuler une réponse réussie
                var transactionReference = Guid.NewGuid().ToString();
                _logger.LogInformation("Mobile withdrawal processed successfully: {TransactionReference}", transactionReference);
                
                return transactionReference;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process mobile withdrawal");
                throw;
            }
        }

        private bool IsValidExpiryDate(string expiryDate)
        {
            // Format attendu: MM/YY
            if (!System.Text.RegularExpressions.Regex.IsMatch(expiryDate, @"^\d{2}/\d{2}$"))
            {
                return false;
            }

            var parts = expiryDate.Split('/');
            var month = int.Parse(parts[0]);
            var year = int.Parse("20" + parts[1]); // Ajouter "20" pour obtenir l'année complète

            if (month < 1 || month > 12)
            {
                return false;
            }

            var currentDate = DateTime.UtcNow;
            var expiryDateObj = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            return expiryDateObj > currentDate;
        }
    }