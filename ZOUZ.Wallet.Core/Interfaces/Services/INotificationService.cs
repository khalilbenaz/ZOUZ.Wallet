namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface INotificationService
{
    Task SendTransactionNotificationAsync(string userId, string message);
    Task SendAlertToAdminsAsync(string message);
}