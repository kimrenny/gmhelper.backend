using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MatHelper.Tests.Services
{
    public class TwoFactorServiceTests
    {
        private readonly Mock<ITwoFactorRepository> _twoFactorRepoMock;
        private readonly Mock<ILogger<ITwoFactorService>> _loggerMock;
        private readonly TwoFactorService _service;

        public TwoFactorServiceTests()
        {
            _twoFactorRepoMock = new Mock<ITwoFactorRepository>();
            _loggerMock = new Mock<ILogger<ITwoFactorService>>();

            _service = new TwoFactorService(_twoFactorRepoMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GenerateTwoFAKeyAsync_CreatesNewKey_WhenNoneExists()
        {
            var userId = Guid.NewGuid();
            string type = "App";

            _twoFactorRepoMock.Setup(r => r.GetTwoFactorAsync(userId, type))
                .ReturnsAsync((UserTwoFactor?)null);

            _twoFactorRepoMock.Setup(r => r.AddTwoFactorAsync(It.IsAny<UserTwoFactor>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GenerateTwoFAKeyAsync(userId, type);

            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(type, result.Type);
            Assert.False(result.IsEnabled);
            Assert.True(result.AlwaysAsk);
            Assert.False(string.IsNullOrEmpty(result.Secret));

            _twoFactorRepoMock.Verify(r => r.AddTwoFactorAsync(result), Times.Once);
        }

        [Fact]
        public async Task GenerateTwoFAKeyAsync_ReturnsExisting_WhenNotExpired()
        {
            var existing = new UserTwoFactor
            {
                UserId = Guid.NewGuid(),
                Type = "App",
                Secret = "secret",
                IsEnabled = false,
                AlwaysAsk = true,
                CreatedAt = DateTime.UtcNow
            };

            _twoFactorRepoMock.Setup(r => r.GetTwoFactorAsync(existing.UserId, existing.Type))
                .ReturnsAsync(existing);

            var result = await _service.GenerateTwoFAKeyAsync(existing.UserId, existing.Type);

            Assert.Equal(existing, result);
            _twoFactorRepoMock.Verify(r => r.AddTwoFactorAsync(It.IsAny<UserTwoFactor>()), Times.Never);
        }

        [Fact]
        public async Task GenerateTwoFAKeyAsync_RemovesOldKey_WhenExpired()
        {
            var oldKey = new UserTwoFactor
            {
                UserId = Guid.NewGuid(),
                Type = "App",
                Secret = "oldsecret",
                IsEnabled = false,
                AlwaysAsk = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-20)
            };

            _twoFactorRepoMock.Setup(r => r.GetTwoFactorAsync(oldKey.UserId, oldKey.Type))
                .ReturnsAsync(oldKey);
            _twoFactorRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _twoFactorRepoMock.Setup(r => r.AddTwoFactorAsync(It.IsAny<UserTwoFactor>())).Returns(Task.CompletedTask);

            var result = await _service.GenerateTwoFAKeyAsync(oldKey.UserId, oldKey.Type);

            Assert.NotNull(result);
            Assert.NotEqual(oldKey.Secret, result.Secret);
            _twoFactorRepoMock.Verify(r => r.Remove(oldKey), Times.Once);
            _twoFactorRepoMock.Verify(r => r.AddTwoFactorAsync(result), Times.Once);
        }

        [Fact]
        public async Task VerifyTwoFACodeAsync_EnablesKey_WhenValid()
        {
            var twoFactor = new UserTwoFactor
            {
                UserId = Guid.NewGuid(),
                Type = "App",
                Secret = "JBSWY3DPEHPK3PXP",
                IsEnabled = false,
                AlwaysAsk = true,
                CreatedAt = DateTime.UtcNow
            };

            _twoFactorRepoMock.Setup(r => r.GetTwoFactorAsync(twoFactor.UserId, twoFactor.Type))
                .ReturnsAsync(twoFactor);

            _twoFactorRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(twoFactor.Secret));
            var code = totp.ComputeTotp();

            var result = await _service.VerifyTwoFACodeAsync(twoFactor.UserId, twoFactor.Type, code);

            Assert.True(result);
            Assert.True(twoFactor.IsEnabled);
            _twoFactorRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyTwoFACodeAsync_Throws_WhenInvalid()
        {
            var twoFactor = new UserTwoFactor
            {
                UserId = Guid.NewGuid(),
                Type = "App",
                Secret = "JBSWY3DPEHPK3PXP",
                IsEnabled = false,
                AlwaysAsk = true,
                CreatedAt = DateTime.UtcNow
            };

            _twoFactorRepoMock.Setup(r => r.GetTwoFactorAsync(twoFactor.UserId, twoFactor.Type))
                .ReturnsAsync(twoFactor);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.VerifyTwoFACodeAsync(twoFactor.UserId, twoFactor.Type, "wrongcode")
            );
        }

        [Fact]
        public void VerifyTotp_ReturnsTrue_ForValidCode()
        {
            var secret = "JBSWY3DPEHPK3PXP";
            var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret));
            var code = totp.ComputeTotp();

            var result = _service.VerifyTotp(secret, code);

            Assert.True(result);
        }

        [Fact]
        public void VerifyTotp_ReturnsFalse_ForInvalidCode()
        {
            var secret = "JBSWY3DPEHPK3PXP";
            var result = _service.VerifyTotp(secret, "123456");

            Assert.False(result);
        }

        [Fact]
        public void GenerateQrCode_ReturnsNonEmptyString()
        {
            var secret = "JBSWY3DPEHPK3PXP";
            var email = "test@example.com";

            var qr = _service.GenerateQrCode(secret, email);

            Assert.False(string.IsNullOrWhiteSpace(qr));
            Assert.Matches("^[A-Za-z0-9+/=]+$", qr);
        }
    }
}
