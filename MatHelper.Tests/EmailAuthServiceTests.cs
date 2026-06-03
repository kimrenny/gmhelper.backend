using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
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
        private readonly Mock<IEmailConfirmationRepository> _emailConfirmationRepoMock = new();
        private readonly Mock<IEmailLoginCodeRepository> _emailLoginCodeRepoMock = new();
        private readonly Mock<ILogger<EmailAuthService>> _loggerMock = new();

        private readonly EmailAuthService _service;

        public EmailAuthServiceTests()
        {
            _service = new EmailAuthService(
                _mailServiceMock.Object,
                _emailConfirmationRepoMock.Object,
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
        public async Task CreateEmailLoginCodeAsync_ShouldReturnValidCodeAndSendEmail()
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
            Assert.True(codeInt >= 100000 && codeInt <= 999999);

            Assert.False(string.IsNullOrWhiteSpace(result.SessionKey));
            Assert.True(result.SessionKey.Length >= 22);

            _emailLoginCodeRepoMock.Verify(x => x.AddCodeAsync(result), Times.Once);
            _mailServiceMock.Verify(x => x.SendIpConfirmationCodeEmailAsync(user.Email, result.Code), Times.Once);
        }

        [Fact]
        public async Task CreateEmailLoginCodeAsync_ShouldFallbackToUnknown_WhenDevicePropertiesAreNull()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = null, Platform = null };

            var result = await _service.CreateEmailLoginCodeAsync(user, device, "127.0.0.1", remember: false);

            Assert.Equal("Unknown", result.UserAgent);
            Assert.Equal("Unknown", result.Platform);
        }

        [Fact]
        public async Task CreateEmailConfirmationTokenAsync_ShouldReturnValidToken()
        {
            var user = CreateTestUser();

            var result = await _service.CreateEmailConfirmationTokenAsync(user);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(user, result.User);
            Assert.False(result.IsUsed);
            Assert.True(Guid.TryParse(result.Token, out _));
            Assert.True(result.ExpirationDate > DateTime.UtcNow.AddMinutes(55));
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldReturnSuccess_WhenRepositoryReturnsSuccess()
        {
            var token = "valid-token";
            _emailConfirmationRepoMock
                .Setup(x => x.ConfirmUserByTokenAsync(token))
                .ReturnsAsync((ConfirmTokenResult.Success, CreateTestUser()));

            var result = await _service.ConfirmEmailAsync(token);

            Assert.Equal(ConfirmTokenResult.Success, result);
            _mailServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldGenerateNewTokenAndSendEmail_WhenTokenIsExpired()
        {
            var expiredToken = "expired-token";
            var user = CreateTestUser();

            _emailConfirmationRepoMock
                .Setup(x => x.ConfirmUserByTokenAsync(expiredToken))
                .ReturnsAsync((ConfirmTokenResult.TokenExpired, user));

            var result = await _service.ConfirmEmailAsync(expiredToken);

            Assert.Equal(ConfirmTokenResult.TokenExpired, result);

            _emailConfirmationRepoMock.Verify(x => x.AddEmailConfirmationTokenAsync(
                It.Is<EmailConfirmationToken>(t => t.UserId == user.Id && !t.IsUsed && t.Token != expiredToken)
            ), Times.Once);
            
            _emailConfirmationRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);

            _mailServiceMock.Verify(x => x.SendConfirmationEmailAsync(user.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldThrowInvalidDataException_WhenRepositoryThrowsInvalidDataException()
        {
            var token = "broken-token";
            _emailConfirmationRepoMock
                .Setup(x => x.ConfirmUserByTokenAsync(token))
                .ThrowsAsync(new InvalidDataException());

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _service.ConfirmEmailAsync(token));
            Assert.Equal("Invalid or expired token", exception.Message);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ShouldThrowGenericException_WhenUnhandledExceptionOccurs()
        {
            var token = "any-token";
            _emailConfirmationRepoMock
                .Setup(x => x.ConfirmUserByTokenAsync(token))
                .ThrowsAsync(new Exception("DB Timeout"));

            var exception = await Assert.ThrowsAsync<Exception>(() => _service.ConfirmEmailAsync(token));
            Assert.Equal("Unknown error occurred during request", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("DB Timeout", exception.InnerException.Message);
        }
    }
}