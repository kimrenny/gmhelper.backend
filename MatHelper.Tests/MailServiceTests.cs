using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MatHelper.BLL.Services;

namespace MatHelper.Tests.BLL
{
    public class MailServiceTests
    {
        private readonly Mock<ILogger<MailService>> _loggerMock;
        private readonly IConfiguration _configuration;
        private readonly MailService _service;

        public MailServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"ClientApp:BaseUrl", "https://test-client.com"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            _loggerMock = new Mock<ILogger<MailService>>();
            _service = new MailService(_configuration, _loggerMock.Object);
        }

        [Fact]
        public async Task SendConfirmationEmailAsync_ShouldThrow_WhenSmtpConfigMissing()
        {
            Environment.SetEnvironmentVariable("SMTP__Host", null);
            Environment.SetEnvironmentVariable("SMTP_Port", null);
            Environment.SetEnvironmentVariable("SMTP__Username", null);
            Environment.SetEnvironmentVariable("SMTP__Password", null);
            Environment.SetEnvironmentVariable("SMTP__From", null);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SendConfirmationEmailAsync("test@test.com", "token123"));
        }

        [Fact]
        public async Task SendConfirmationEmailAsync_ShouldThrow_WhenPortInvalid()
        {
            Environment.SetEnvironmentVariable("SMTP__Host", "smtp.test.com");
            Environment.SetEnvironmentVariable("SMTP_Port", "not-a-port");
            Environment.SetEnvironmentVariable("SMTP__Username", "user");
            Environment.SetEnvironmentVariable("SMTP__Password", "pass");
            Environment.SetEnvironmentVariable("SMTP__From", "noreply@test.com");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SendConfirmationEmailAsync("test@test.com", "token123"));
        }

        [Fact]
        public async Task SendPasswordRecoveryEmailAsync_ShouldThrow_WhenClientBaseUrlMissing()
        {
            var configWithoutUrl = new ConfigurationBuilder().Build();
            var service = new MailService(configWithoutUrl, _loggerMock.Object);

            Environment.SetEnvironmentVariable("SMTP__Host", "smtp.test.com");
            Environment.SetEnvironmentVariable("SMTP_Port", "587");
            Environment.SetEnvironmentVariable("SMTP__Username", "user");
            Environment.SetEnvironmentVariable("SMTP__Password", "pass");
            Environment.SetEnvironmentVariable("SMTP__From", "noreply@test.com");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendPasswordRecoveryEmailAsync("test@test.com", "recoveryToken"));
        }

        [Fact]
        public async Task SendIpConfirmationCodeEmailAsync_ShouldThrow_WhenConfigInvalid()
        {
            Environment.SetEnvironmentVariable("SMTP__Host", "");
            Environment.SetEnvironmentVariable("SMTP_Port", "");
            Environment.SetEnvironmentVariable("SMTP__Username", "");
            Environment.SetEnvironmentVariable("SMTP__Password", "");
            Environment.SetEnvironmentVariable("SMTP__From", "");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SendIpConfirmationCodeEmailAsync("test@test.com", "123456"));
        }
    }
}
