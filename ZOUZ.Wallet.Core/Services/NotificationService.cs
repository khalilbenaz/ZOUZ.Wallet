using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Core.Services;

/// <summary>
/// Service de notification qui fait partie de la couche Core (logique métier)
/// Cette classe gère la logique de notification indépendamment des mécanismes d'envoi
/// </summary>
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
        _logger.LogInformation("Envoi d'une notification de transaction à l'utilisateur {UserId}: {Message}", userId, message);
            
        // Logique de décision sur comment envoyer la notification
        // Cette implémentation délègue simplement à d'autres services
        await _emailService.SendEmailAsync("user@example.com", "Notification de transaction", message);
        await _smsService.SendSmsAsync("+212600000000", message);
    }

    public async Task SendAlertToAdminsAsync(string message)
    {
        _logger.LogInformation("Envoi d'alerte aux administrateurs: {Message}", message);
            
        // Logique pour déterminer quels administrateurs doivent recevoir l'alerte
        await _emailService.SendEmailAsync("admin@walletapi.ma", "Alerte de sécurité", message);
    }
}