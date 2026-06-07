using MatHelper.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace MatHelper.IntegrationTests.Services
{
    public class MockMailService : IMailService
    {
        private readonly ILogger<MockMailService> _logger;

        public MockMailService(ILogger<MockMailService> logger)
        {
            _logger = logger;
        }

        public Task SendRegistrationCodeEmailAsync(string toEmail, string code)
        {
            _logger.LogInformation($"[MOCK MAIL] Registration code to {toEmail}, code: {code}");
            return Task.CompletedTask;
        }

        public Task SendConfirmationEmailAsync(string toEmail)
        {
            _logger.LogInformation($"[MOCK MAIL] Confirmation email to {toEmail}");
            return Task.CompletedTask;
        }

        public Task SendPasswordRecoveryEmailAsync(string toEmail, string token)
        {
            _logger.LogInformation($"[MOCK MAIL] Password recovery email to {toEmail}, token: {token}");
            return Task.CompletedTask;
        }

        public Task SendIpConfirmationCodeEmailAsync(string toEmail, string code)
        {
            _logger.LogInformation($"[MOCK MAIL] IP confirmation email to {toEmail}, code: {code}");
            return Task.CompletedTask;
        }

        public bool ValidateEmailFormatAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.");

            var parts = email.Split('@');

            if (parts.Length != 2)
                throw new ArgumentException("Invalid email format.");

            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                throw new ArgumentException("Invalid email format.");

            return true;
        }
    }
}