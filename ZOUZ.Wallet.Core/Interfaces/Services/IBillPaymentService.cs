using ZOUZ.Wallet.Core.DTOs.Base;
using ZOUZ.Wallet.Core.DTOs.Responses;

namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface IBillPaymentService
{
    Task<BillVerificationResult> VerifyBillAsync(string billerName, string billerReference, string customerReference, decimal amount);
    Task<string> PayBillAsync(string billerName, string billerReference, string customerReference, decimal amount, string billType);
}