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

        public Task SendConfirmationEmailAsync(string toEmail, string token)
        {
            _logger.LogInformation($"[MOCK MAIL] Confirmation email to {toEmail} with token {token}");
            return Task.CompletedTask;
        }

        public Task SendPasswordRecoveryEmailAsync(string toEmail, string token)
        {
            _logger.LogInformation($"[MOCK MAIL] Password recovery email to {toEmail} with token {token}");
            return Task.CompletedTask;
        }

        public Task SendIpConfirmationCodeEmailAsync(string toEmail, string code)
        {
            _logger.LogInformation($"[MOCK MAIL] IP confirmation code email to {toEmail} with code {code}");
            return Task.CompletedTask;
        }
    }
}
