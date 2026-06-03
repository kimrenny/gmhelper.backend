using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Moq;
using Xunit;

namespace MatHelper.Tests
{
    public class TwoFactorAuthServiceTests
    {
        private readonly Mock<ITwoFactorService> _twoFactorServiceMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IAppTwoFactorSessionRepository> _twoFactorSessionRepositoryMock = new();

        private readonly TwoFactorAuthService _service;

        public TwoFactorAuthServiceTests()
        {
            _service = new TwoFactorAuthService(
                _twoFactorServiceMock.Object,
                _userRepositoryMock.Object,
                _twoFactorSessionRepositoryMock.Object
            );
        }

        private User CreateTestUser(bool isBlocked = false) => new()
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            Role = "User",
            RegistrationDate = DateTime.UtcNow,
            IsBlocked = isBlocked
        };

        private AppTwoFactorSession CreateTestSession(Guid userId, bool isUsed = false, int expirationMinutesOffset = 10) => new()
        {
            UserId = userId,
            SessionKey = "valid_session_key",
            Expiration = DateTime.UtcNow.AddMinutes(expirationMinutesOffset),
            IpAddress = "127.0.0.1",
            UserAgent = "Mozilla",
            Platform = "Windows",
            Remember = true,
            IsUsed = isUsed
        };

        [Fact]
        public async Task CreateTwoFactorSessionAsync_ShouldReturnValidSessionAndSaveToDb()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = "Mozilla", Platform = "Windows" };
            var ip = "127.0.0.1";

            var result = await _service.CreateTwoFactorSessionAsync(user, device, ip, remember: true);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.UserId);
            Assert.False(string.IsNullOrWhiteSpace(result.SessionKey));
            Assert.True(result.SessionKey.Length >= 22);
            Assert.True(result.Expiration > DateTime.UtcNow.AddMinutes(9));
            Assert.Equal(ip, result.IpAddress);
            Assert.Equal(device.UserAgent, result.UserAgent);
            Assert.Equal(device.Platform, result.Platform);
            Assert.True(result.Remember);
            Assert.False(result.IsUsed);

            _twoFactorSessionRepositoryMock.Verify(x => x.AddSessionAsync(result), Times.Once);
        }

        [Fact]
        public async Task CreateTwoFactorSessionAsync_ShouldFallbackToUnknown_WhenDevicePropertiesAreNull()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = null, Platform = null };

            var result = await _service.CreateTwoFactorSessionAsync(user, device, "127.0.0.1", remember: false);

            Assert.Equal("Unknown", result.UserAgent);
            Assert.Equal("Unknown", result.Platform);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenSessionNotFound()
        {
            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(It.IsAny<string>())).ReturnsAsync((AppTwoFactorSession)null!);

            var result = await _service.ValidateTwoFactorSessionAsync("invalid_key", "123456");

            Assert.False(result.Success);
            Assert.Equal("Invalid or expired session key.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenSessionIsAlreadyUsed()
        {
            var userId = Guid.NewGuid();
            var session = CreateTestSession(userId, isUsed: true);
            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, "123456");

            Assert.False(result.Success);
            Assert.Equal("Invalid or expired session key.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenSessionIsExpired()
        {
            var userId = Guid.NewGuid();
            var session = CreateTestSession(userId, isUsed: false, expirationMinutesOffset: -5);
            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, "123456");

            Assert.False(result.Success);
            Assert.Equal("Invalid or expired session key.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();
            var session = CreateTestSession(userId);
            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((User)null!);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, "123456");

            Assert.False(result.Success);
            Assert.Equal("User not found.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenUserIsBlocked()
        {
            var user = CreateTestUser(isBlocked: true);
            var session = CreateTestSession(user.Id);
            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, "123456");

            Assert.False(result.Success);
            Assert.Equal("User is banned.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenTwoFactorRecordNotFound()
        {
            var user = CreateTestUser();
            var session = CreateTestSession(user.Id);
            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _twoFactorServiceMock.Setup(x => x.GetTwoFactorAsync(user.Id, "totp")).ReturnsAsync((UserTwoFactor)null!);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, "123456");

            Assert.False(result.Success);
            Assert.Equal("2FA not enabled.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenTwoFactorIsDisabled()
        {
            var user = CreateTestUser();
            var session = CreateTestSession(user.Id);
            var userTwoFactor = new UserTwoFactor { IsEnabled = false, Secret = "secret" };
            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _twoFactorServiceMock.Setup(x => x.GetTwoFactorAsync(user.Id, "totp")).ReturnsAsync(userTwoFactor);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, "123456");

            Assert.False(result.Success);
            Assert.Equal("2FA not enabled.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenTwoFactorSecretIsNull()
        {
            var user = CreateTestUser();
            var session = CreateTestSession(user.Id);
            var userTwoFactor = new UserTwoFactor { IsEnabled = true, Secret = null! };
            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _twoFactorServiceMock.Setup(x => x.GetTwoFactorAsync(user.Id, "totp")).ReturnsAsync(userTwoFactor);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, "123456");

            Assert.False(result.Success);
            Assert.Equal("Invalid key.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnFailure_WhenTotpCodeIsInvalid()
        {
            var user = CreateTestUser();
            var session = CreateTestSession(user.Id);
            var userTwoFactor = new UserTwoFactor { IsEnabled = true, Secret = "secret" };
            var invalidCode = "000000";

            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _twoFactorServiceMock.Setup(x => x.GetTwoFactorAsync(user.Id, "totp")).ReturnsAsync(userTwoFactor);
            _twoFactorServiceMock.Setup(x => x.VerifyTotp(userTwoFactor.Secret, invalidCode)).Returns(false);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, invalidCode);

            Assert.False(result.Success);
            Assert.Equal("Invalid 2FA code.", result.Error);
        }

        [Fact]
        public async Task ValidateTwoFactorSessionAsync_ShouldReturnSuccess_WhenTotpCodeIsValid()
        {
            var user = CreateTestUser();
            var session = CreateTestSession(user.Id);
            var userTwoFactor = new UserTwoFactor { IsEnabled = true, Secret = "secret" };
            var validCode = "123456";

            _twoFactorSessionRepositoryMock.Setup(x => x.GetBySessionKeyAsync(session.SessionKey)).ReturnsAsync(session);
            _userRepositoryMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _twoFactorServiceMock.Setup(x => x.GetTwoFactorAsync(user.Id, "totp")).ReturnsAsync(userTwoFactor);
            _twoFactorServiceMock.Setup(x => x.VerifyTotp(userTwoFactor.Secret, validCode)).Returns(true);

            var result = await _service.ValidateTwoFactorSessionAsync(session.SessionKey, validCode);

            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.Equal(user.Id, result.UserId);
        }
    }
}