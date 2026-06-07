using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatHelper.Tests
{
    public class EmailAuthServiceTests
    {
        private readonly Mock<IMailService> _mailServiceMock = new();
        private readonly Mock<IEmailLoginCodeRepository> _emailLoginCodeRepoMock = new();
        private readonly Mock<ILogger<EmailAuthService>> _loggerMock = new();

        private readonly EmailAuthService _service;

        public EmailAuthServiceTests()
        {
            _service = new EmailAuthService(
                _mailServiceMock.Object,
                _emailLoginCodeRepoMock.Object,
                _loggerMock.Object
            );
        }

        private User CreateTestUser() => new()
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "test_hash",
            Role = "User",
            RegistrationDate = DateTime.UtcNow
        };

        [Fact]
        public async Task CreateEmailLoginCodeAsync_ShouldReturnValidCode()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = "Mozilla/5.0", Platform = "Windows" };
            var ip = "192.168.1.1";

            var result = await _service.CreateEmailLoginCodeAsync(user, device, ip, remember: true);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(ip, result.IpAddress);
            Assert.Equal(device.UserAgent, result.UserAgent);
            Assert.Equal(device.Platform, result.Platform);
            Assert.True(result.Remember);
            Assert.False(result.IsUsed);
            Assert.True(result.Expiration > DateTime.UtcNow);

            Assert.True(int.TryParse(result.Code, out int codeInt));
            Assert.InRange(codeInt, 100000, 999999);

            Assert.False(string.IsNullOrWhiteSpace(result.SessionKey));

            _emailLoginCodeRepoMock.Verify(x => x.AddCodeAsync(result), Times.Once);
            _mailServiceMock.Verify(x => x.SendIpConfirmationCodeEmailAsync(user.Email, result.Code), Times.Once);
        }

        [Fact]
        public async Task CreateEmailLoginCodeAsync_ShouldUseUnknownForNullDevice()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = null, Platform = null };

            var result = await _service.CreateEmailLoginCodeAsync(user, device, "127.0.0.1", remember: false);

            Assert.Equal("Unknown", result.UserAgent);
            Assert.Equal("Unknown", result.Platform);
        }

        [Fact]
        public async Task CreateEmailRegisterCodeAsync_ShouldCreateAndSendEmail()
        {
            var dto = new RegisterRequestDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                CaptchaToken = "captcha"
            };

            var device = new DeviceInfo { UserAgent = "UA", Platform = "Windows" };
            var ip = "127.0.0.1";

            var result = await _service.CreateEmailRegisterCodeAsync(dto, device, ip);

            Assert.NotNull(result);
            Assert.Equal(dto.Email, result.Email);

            _emailLoginCodeRepoMock.Verify(x => x.InvalidateActiveCodesByEmailAsync(dto.Email), Times.Once);
            _emailLoginCodeRepoMock.Verify(x => x.AddCodeAsync(result), Times.Once);
            _mailServiceMock.Verify(x => x.SendRegistrationCodeEmailAsync(dto.Email, result.Code), Times.Once);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldReturnRepositoryResult()
        {
            var email = "test@example.com";
            var code = "123456";

            _emailLoginCodeRepoMock
                .Setup(x => x.ConfirmByEmailAndCodeAsync(email, code))
                .ReturnsAsync(ConfirmTokenResult.Success);

            var result = await _service.ConfirmEmailAsync(email, code);

            Assert.Equal(ConfirmTokenResult.Success, result);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldThrowWrappedException_OnError()
        {
            var email = "test@example.com";
            var code = "bad";

            _emailLoginCodeRepoMock
                .Setup(x => x.ConfirmByEmailAndCodeAsync(email, code))
                .ThrowsAsync(new Exception("DB fail"));

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.ConfirmEmailAsync(email, code));

            Assert.Equal("Unknown error occurred during request", ex.Message);
            Assert.NotNull(ex.InnerException);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldWrapInvalidDataException()
        {
            var email = "test@example.com";
            var code = "bad";

            _emailLoginCodeRepoMock
                .Setup(x => x.ConfirmByEmailAndCodeAsync(email, code))
                .ThrowsAsync(new InvalidDataException());

            var ex = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _service.ConfirmEmailAsync(email, code));

            Assert.Equal("Invalid or expired token", ex.Message);
        }
    }
}