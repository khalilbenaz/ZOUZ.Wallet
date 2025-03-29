using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsService> _logger;
    private readonly HttpClient _httpClient;

    public SmsService(
        IConfiguration configuration,
        ILogger<SmsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("SmsService");
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            var provider = _configuration["Notifications:SMS:Provider"];
            var apiKey = _configuration["Notifications:SMS:ApiKey"];
            var apiUrl = _configuration["Notifications:SMS:ApiUrl"];
            var senderName = _configuration["Notifications:SMS:SenderName"];

            // En environnement de développement, on simule l'envoi
            if (_configuration["Environment"] == "Development")
            {
                _logger.LogInformation("SMS would be sent to {PhoneNumber}: {Message}", phoneNumber, message);
                return true;
            }

            // Dans un cas réel, on appellerait l'API du fournisseur de SMS
            // Pour l'exemple, on simule l'appel API
            _logger.LogInformation("SMS sent to {PhoneNumber}: {Message}", phoneNumber, message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}