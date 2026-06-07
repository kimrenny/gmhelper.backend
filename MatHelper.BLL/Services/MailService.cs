using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using MatHelper.BLL.Interfaces;
using System.Net;
using Microsoft.Extensions.Logging;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Interfaces;
using MatHelper.BLL.MailTemplates;

namespace MatHelper.BLL.Services
{
    public class MailService : IMailService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserManagementService _userManagementService;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public MailService(IUserRepository userRepository, IUserManagementService userManagementService, IConfiguration configuration, ILogger<MailService> logger)
        {
            _userRepository = userRepository;
            _userManagementService = userManagementService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendRegistrationCodeEmailAsync(string toEmail, string code)
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

            _logger.LogInformation($"Attempting to send IP confirmation code email to {toEmail} using SMTP server {smtpHost} on port {smtpPort}.");

            try
            {
                var fromAddress = new MailAddress(smtpFrom, "GMHelper");
                var toAddress = new MailAddress(toEmail);

                var clientBaseUrl = _configuration["ClientApp:BaseUrl"];
                if (string.IsNullOrWhiteSpace(clientBaseUrl))
                {
                    throw new InvalidOperationException("Client application base URL is not configured.");
                }

                var mainLink = $"{clientBaseUrl.TrimEnd('/')}/";

                var template = RegisterCodeMailProvider.Get("en", mainLink, code);

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = template.Subject,
                        Body = template.Body,
                        IsBodyHtml = true
                    };

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send registration code email to {toEmail}.");
                throw;
            }
        }

        public async Task SendConfirmationEmailAsync(string toEmail)
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

            _logger.LogInformation($"Attempting to send registration success email to {toEmail} using SMTP server {smtpHost} on port {smtpPort}.");

            try
            {
                var fromAddress = new MailAddress(smtpFrom, "GMHelper");
                var toAddress = new MailAddress(toEmail);
                var subject = "Welcome to GMHelper";

                var clientBaseUrl = _configuration["ClientApp:BaseUrl"];
                if (string.IsNullOrWhiteSpace(clientBaseUrl))
                {
                    throw new InvalidOperationException("Client application base URL is not configured.");
                }

                var mainLink = $"{clientBaseUrl.TrimEnd('/')}/";

                var body = $@"
                <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                    <h2 style='text-align:center;'>
                        <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                            <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                        </a>
                    </h2>
                    <p style='color:#fff;'>Hello,</p>
                    <p style='color:#fff;'>Welcome to GMHelper!</p>
                    <p style='color:#fff;'>Your registration has been completed successfully and your email address has been verified.</p>
                    <p style='color:#fff;'>You can now sign in and start using all available features of the platform.</p>
                    <p style='text-align:center; margin:30px 0;'>
                        <a href='{mainLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>Open GMHelper</a>
                    </p>
                    <p style='color:#fff;'>If you did not create this account, please contact support as soon as possible.</p>
                    <hr style='border-color:#444;'/>
                    <footer style='text-align:center; font-size:12px; color:#666;'>
                        &copy; {DateTime.Now.Year} GMHelper. All rights reserved.
                    </footer>
                </div>";

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation($"Registration success email successfully sent to {toEmail}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send registration success email to {toEmail}.");
                throw;
            }
        }

        public async Task SendPasswordRecoveryEmailAsync(string toEmail, string token)
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

            _logger.LogInformation($"Attempting to send password recovery email to {toEmail} using SMTP server {smtpHost} on port {smtpPort}.");

            try
            {
                var fromAddress = new MailAddress(smtpFrom, "GMHelper");
                var toAddress = new MailAddress(toEmail);

                var clientBaseUrl = _configuration["ClientApp:BaseUrl"];
                if (string.IsNullOrWhiteSpace(clientBaseUrl))
                {
                    throw new InvalidOperationException("Client application base URL is not configured.");
                }

                var recoveryLink = $"{clientBaseUrl.TrimEnd('/')}/recover?token={token}";
                var mainLink = $"{clientBaseUrl.TrimEnd('/')}/";

                var language = await _userManagementService.GetUserLanguageByEmail(toEmail);

                var template = PasswordRecoveryMailProvider.Get(language, mainLink, recoveryLink);

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = template.Subject,
                        Body = template.Body,
                        IsBodyHtml = true
                    };

                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation($"Password recovery email successfully sent to {toEmail}");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to send password recovery email to {toEmail}");
                throw;
            }
        }

        public async Task SendIpConfirmationCodeEmailAsync(string toEmail, string code)
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

            _logger.LogInformation($"Attempting to send IP confirmation code email to {toEmail} using SMTP server {smtpHost} on port {smtpPort}.");

            try
            {
                var fromAddress = new MailAddress(smtpFrom, "GMHelper");
                var toAddress = new MailAddress(toEmail);

                var clientBaseUrl = _configuration["ClientApp:BaseUrl"];
                if (string.IsNullOrWhiteSpace(clientBaseUrl))
                {
                    throw new InvalidOperationException("Client application base URL is not configured.");
                }

                var mainLink = $"{clientBaseUrl.TrimEnd('/')}/";

                var language = await _userManagementService.GetUserLanguageByEmail(toEmail);

                var template = IpConfirmationMailProvider.Get(language, mainLink, code);

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = template.Subject,
                        Body = template.Body,
                        IsBodyHtml = true
                    };

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation($"IP confirmation code email successfully sent to {toEmail}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send IP confirmation code email to {toEmail}.");
                throw;
            }
        }

        public bool ValidateEmailFormatAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.");

            email = email.Trim();

            var parts = email.Split('@');

            if (parts.Length != 2)
                throw new ArgumentException("Invalid email format.");

            var local = parts[0];
            var domain = parts[1];

            if (string.IsNullOrWhiteSpace(local))
                throw new ArgumentException("Invalid email format.");

            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Invalid email format.");

            var domainParts = domain.Split('.');

            if (domainParts.Length < 2)
                throw new ArgumentException("Invalid email domain.");

            return true;
        }
    }
}