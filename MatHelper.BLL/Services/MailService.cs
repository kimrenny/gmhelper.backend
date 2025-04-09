using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MatHelper.BLL.Interfaces;
using System.Net;
using Microsoft.Extensions.Logging;

namespace MatHelper.BLL.Services
{
    public class MailService : IMailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public MailService(IConfiguration configuration, ILogger<MailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendConfirmationEmailAsync(string toEmail, string token)
        {
            var smtpHost = Environment.GetEnvironmentVariable("SMTP__Host");
            var smtpPortString = Environment.GetEnvironmentVariable("SMTP_Port");
            var smtpUsername = Environment.GetEnvironmentVariable("SMTP__Username");
            var smtpPassword = Environment.GetEnvironmentVariable("SMTP__Password");
            var smtpFrom = Environment.GetEnvironmentVariable("SMTP__From");

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(smtpFrom))
            {
                throw new InvalidOperationException("SMTP configuration is missing in the environment variables.");
            }

            if (!int.TryParse(smtpPortString, out var smtpPort))
            {
                throw new InvalidOperationException("Invalid SMTP port value.");
            }

            _logger.LogInformation($"Attempting to send confirmation email to {toEmail} using SMTP server {smtpHost} on port {smtpPort}.");

            try
            {
                var fromAddress = new MailAddress(smtpFrom, "GMHelper");
                var toAddress = new MailAddress(toEmail);
                var subject = "Email Confirmation";
                var body = $"Please confirm your email by clicking the following link: https://localhost:4200/confirm?token={token}";

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false
                    };

                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation($"Confirmation email successfully sent to {toEmail}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send confirmation email to {toEmail}.");
                throw;
            }

        }
    }
}