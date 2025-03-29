using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface IFraudDetectionService
{
    Task<bool> IsSuspiciousDepositAsync(Guid walletId, decimal amount, PaymentMethod method);
    Task<bool> IsSuspiciousWithdrawalAsync(Guid walletId, decimal amount, PaymentMethod method);
    Task<bool> IsSuspiciousTransferAsync(Guid sourceWalletId, Guid destinationWalletId, decimal amount);
}