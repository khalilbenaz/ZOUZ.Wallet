using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Infrastructure.Services;

public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["Notifications:Email:SmtpServer"];
                var smtpPort = int.Parse(_configuration["Notifications:Email:SmtpPort"]);
                var smtpUsername = _configuration["Notifications:Email:SmtpUsername"];
                var smtpPassword = _configuration["Notifications:Email:SmtpPassword"];
                var fromEmail = _configuration["Notifications:Email:FromEmail"];
                var fromName = _configuration["Notifications:Email:FromName"];

                using (var client = new SmtpClient(smtpServer))
                {
                    client.Port = smtpPort;
                    client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                    client.EnableSsl = true;

                    var message = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    message.To.Add(new MailAddress(to));

                    // En environnement de développement, on simule l'envoi
                    if (_configuration["Environment"] == "Development")
                    {
                        _logger.LogInformation("Email would be sent to {To} with subject: {Subject}", to, subject);
                        return true;
                    }

                    await client.SendMailAsync(message);
                    _logger.LogInformation("Email sent to {To} with subject: {Subject}", to, subject);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
                return false;
            }
        }
    }