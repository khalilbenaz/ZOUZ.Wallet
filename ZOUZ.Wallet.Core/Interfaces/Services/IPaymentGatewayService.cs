namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface IPaymentGatewayService
{
    Task<string> ProcessCardPaymentAsync(string cardNumber, string cardHolderName, string expiryDate, string cvv, decimal amount, string currency);
    Task<string> VerifyMobilePaymentAsync(string reference, string provider, decimal amount);
    Task<string> ProcessBankWithdrawalAsync(string accountNumber, string bankName, decimal amount, string currency);
    Task<string> ProcessMobileWithdrawalAsync(string phoneNumber, string provider, decimal amount);
}