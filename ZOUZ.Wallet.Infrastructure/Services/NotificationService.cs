using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        ISmsService smsService,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task SendTransactionNotificationAsync(string userId, string message)
    {
        _logger.LogInformation("Sending transaction notification to user {UserId}: {Message}", userId, message);
            
        // Dans un cas réel, on récupérerait les préférences de notification de l'utilisateur
        // et on enverrait la notification via le canal choisi (email, SMS, push, etc.)
            
        // Pour l'exemple, on simule l'envoi d'un SMS
        await _smsService.SendSmsAsync("+212600000000", message);
    }

    public async Task SendAlertToAdminsAsync(string message)
    {
        _logger.LogInformation("Sending alert to admins: {Message}", message);
            
        // Dans un cas réel, on récupérerait la liste des administrateurs
        // et on leur enverrait un email d'alerte
            
        // Pour l'exemple, on simule l'envoi d'un email
        await _emailService.SendEmailAsync("admin@walletapi.ma", "Alerte de sécurité", message);
    }
}