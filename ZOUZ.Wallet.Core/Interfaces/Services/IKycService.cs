using ZOUZ.Wallet.Core.DTOs.Base;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface IKycService
{
    Task<bool> InitiateBasicVerificationAsync(Guid walletId, string cinNumber);
    Task<bool> VerifyIdentityAsync(Guid walletId, VerifyIdentityRequest request);
    Task<bool> UpgradeKycLevelAsync(Guid walletId, KycLevel newLevel);
    Task<KycVerificationStatus> CheckVerificationStatusAsync(Guid walletId);
}