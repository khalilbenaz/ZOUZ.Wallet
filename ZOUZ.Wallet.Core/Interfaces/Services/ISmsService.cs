namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message);
}