using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ZOUZ.Wallet.Core.DTOs.Base;
using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Infrastructure.Services;

public class FraudDetectionService : IFraudDetectionService
    {
        private readonly ILogger<FraudDetectionService> _logger;

        public FraudDetectionService(ILogger<FraudDetectionService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsSuspiciousDepositAsync(Guid walletId, decimal amount, PaymentMethod method)
        {
            // Logique simplifiée pour la détection de fraude
            // Dans un cas réel, on utiliserait des algorithmes plus sophistiqués
            
            // Exemple : Les dépôts de plus de 10000 MAD sont considérés comme suspects
            if (amount > 10000)
            {
                _logger.LogWarning("Suspicious deposit detected: {Amount} to wallet {WalletId}", amount, walletId);
                return true;
            }
            
            return false;
        }

        public async Task<bool> IsSuspiciousWithdrawalAsync(Guid walletId, decimal amount, PaymentMethod method)
        {
            // Exemple : Les retraits de plus de 5000 MAD sont considérés comme suspects
            if (amount > 5000)
            {
                _logger.LogWarning("Suspicious withdrawal detected: {Amount} from wallet {WalletId}", amount, walletId);
                return true;
            }
            
            return false;
        }

        public async Task<bool> IsSuspiciousTransferAsync(Guid sourceWalletId, Guid destinationWalletId, decimal amount)
        {
            // Exemple : Les transferts de plus de 7000 MAD sont considérés comme suspects
            if (amount > 7000)
            {
                _logger.LogWarning("Suspicious transfer detected: {Amount} from wallet {SourceWalletId} to {DestinationWalletId}", 
                    amount, sourceWalletId, destinationWalletId);
                return true;
            }
            
            return false;
        }
    }

    
    
    

   
    

   
    

    
    