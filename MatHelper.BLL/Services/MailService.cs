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

                var clientBaseUrl = _configuration["ClientApp:BaseUrl"];
                if(string.IsNullOrWhiteSpace(clientBaseUrl))
                {
                    throw new InvalidOperationException("Client application base URL is not configured.");
                }

                var confirmationLink = $"{clientBaseUrl.TrimEnd('/')}/confirm?token={token}";
                var mainLink = $"{clientBaseUrl.TrimEnd('/')}/";

                var body = $@"
                <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                    <h2 style='text-align:center;'>
                        <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                            <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                        </a>
                    </h2>
                    <p style='color:#fff;'>Hello,</p>
                    <p style='color:#fff;'>Thank you for registering with GMHelper. In order to complete your registration, please confirm your email address by clicking the button below:</p>
                    <p style='text-align:center;'>
                        <a href='{confirmationLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>Confirm Email</a>
                    </p>
                    <p style='color:#fff;'>If the button doesn't work, you can copy and paste the following link into your browser's address bar:</p>
                    <p style='color:#fff; text-align:center;'>
                        <a href='{confirmationLink}' style='color:#C444FF;'>{confirmationLink}</a>
                    </p>
                    <p style='color:#fff;'>Please note that if you do not confirm your email within one hour, your account will be automatically deleted for security reasons.</p>
                    <p style='color:#fff;'>If you did not register for GMHelper, please disregard this message.</p>
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

                    _logger.LogInformation($"Confirmation email successfully sent to {toEmail}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send confirmation email to {toEmail}.");
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
                var subject = "Password Recovery";

                var clientBaseUrl = _configuration["ClientApp:BaseUrl"];
                if (string.IsNullOrWhiteSpace(clientBaseUrl))
                {
                    throw new InvalidOperationException("Client application base URL is not configured.");
                }

                var recoveryLink = $"{clientBaseUrl.TrimEnd('/')}/recover?token={token}";
                var mainLink = $"{clientBaseUrl.TrimEnd('/')}/";

                var body = $@"
                <div style='background-color:#000; color:#fff; font-family:Arial, sans-serif; max-width:600px; margin:auto; padding:20px; border-radius:10px;'>
                    <h2 style='text-align:center;'>
                        <a href='{mainLink}' style='text-decoration:none; font-size:28px;'>
                            <span style='color:#C444FF;'>GM</span><span style='color:#FFFFFF;'>Helper</span>
                        </a>
                    </h2>
                    <p style='color:#fff;'>Hello!</p>
                    <p style='color:#fff;'>We received a request to reset your password. Click the button below to proceed:</p>
                    <p style='text-align:center;'>
                        <a href='{recoveryLink}' style='display:inline-block; padding:12px 24px; background-color:#C444FF; color:#fff; text-decoration:none; border-radius:5px;'>Reset Password</a>
                    </p>
                    <p style='color:#fff;'>If the button doesn't work, you can copy and paste the following link into your browser:</p>
                    <p style='color:#fff; text-align:center;'>
                        <a href='{recoveryLink}' style='color:#C444FF;'>{recoveryLink}</a>
                    </p>
                    <p style='color:#fff;'>This link will expire in 15 minutes. If you did not request a password reset, you can safely ignore this message.</p>
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

                    _logger.LogInformation($"Password recovery email successfully sent to {toEmail}");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Failed to send password recovery email to {toEmail}");
                throw;
            }
        }
    }
    
}