using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.DTOs.Base;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Infrastructure.ExternalServices;

    /// <summary>
    /// Implémentation qui se connecte directement aux APIs externes de paiement de factures
    /// </summary>
    public class BillPaymentService : IBillPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BillPaymentService> _logger;
        private readonly HttpClient _httpClient;

        public BillPaymentService(
            IConfiguration configuration,
            ILogger<BillPaymentService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("BillPayment");
        }

        public async Task<BillVerificationResult> VerifyBillAsync(string billerName, string billerReference, string customerReference, decimal amount)
        {
            _logger.LogInformation("[ExternalService] Verifying bill: {BillerName}, Ref: {BillerReference}, Customer: {CustomerReference}, Amount: {Amount}",
                billerName, billerReference, customerReference, amount);
            
            try {
                // Simuler un appel API au fournisseur de services (Maroc Telecom, LYDEC, etc.)
                // Dans un environnement de production, ce code ferait un appel HTTP à l'API du fournisseur
                
                // Retourner un résultat fictif positif
                return new BillVerificationResult 
                {
                    IsValid = true,
                    Message = "Facture vérifiée avec succès",
                    DueDate = DateTime.Now.AddDays(15)
                };
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error verifying bill with external service");
                throw;
            }
        }

        public async Task<string> PayBillAsync(string billerName, string billerReference, string customerReference, decimal amount, string billType)
        {
            _logger.LogInformation("[ExternalService] Paying bill: {BillerName}, Type: {BillType}, Ref: {BillerReference}, Amount: {Amount}",
                billerName, billType, billerReference, amount);
            
            try {
                // Récupérer l'URL d'API appropriée en fonction du type de facture
                string apiUrl = billType.ToLower() switch {
                    "telecom" => _configuration["BillPayment:Providers:Telecom:ApiUrl"],
                    "water" => _configuration["BillPayment:Providers:Water:ApiUrl"],
                    "electricity" => _configuration["BillPayment:Providers:Electricity:ApiUrl"], 
                    "taxes" => _configuration["BillPayment:Providers:Taxes:ApiUrl"],
                    _ => throw new ArgumentException($"Type de facture non supporté: {billType}")
                };
                
                // Simuler un appel API pour payer la facture
                // Dans un environnement de production, ce code ferait un appel HTTP à l'API du fournisseur
                
                // Retourner un ID de transaction fictif
                return Guid.NewGuid().ToString();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error paying bill with external service");
                throw;
            }
        }
    }