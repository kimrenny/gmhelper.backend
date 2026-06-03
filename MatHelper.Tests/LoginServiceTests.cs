using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatHelper.Tests
{
    public class LoginServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<ITokenGeneratorService> _tokenGeneratorMock = new();
        private readonly Mock<ISecurityService> _securityServiceMock = new();
        private readonly Mock<ILogger<LoginService>> _loggerMock = new();

        private readonly LoginService _service;

        public LoginServiceTests()
        {
            _service = new LoginService(
                _userRepositoryMock.Object,
                _tokenGeneratorMock.Object,
                _securityServiceMock.Object,
                _loggerMock.Object
            );
        }

        private User CreateTestUser(bool isActive = true, bool isBlocked = false) => new()
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "testuser",
            PasswordHash = "correct_hash",
            Role = "User",
            RegistrationDate = DateTime.UtcNow,
            IsActive = isActive,
            IsBlocked = isBlocked,
            LoginTokens = new List<LoginToken>()
        };

        [Fact]
        public async Task ValidateUserCredentialsAsync_ShouldReturnUser_WhenCredentialsAreValid()
        {
            var email = "user@example.com";
            var password = "CorrectPassword123!";
            var user = CreateTestUser();

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(user);
            _securityServiceMock.Setup(x => x.VerifyPassword(password, user.PasswordHash)).Returns(true);

            var result = await _service.ValidateUserCredentialsAsync(email, password);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task ValidateUserCredentialsAsync_ShouldThrowInvalidOperationException_WhenUserNotFound()
        {
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.ValidateUserCredentialsAsync("nonexistent@example.com", "any_pass"));
            
            Assert.Equal("User not found.", exception.Message);
        }

        [Fact]
        public async Task ValidateUserCredentialsAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsIncorrect()
        {
            var email = "user@example.com";
            var password = "WrongPassword!";
            var user = CreateTestUser();

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(user);
            _securityServiceMock.Setup(x => x.VerifyPassword(password, user.PasswordHash)).Returns(false);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _service.ValidateUserCredentialsAsync(email, password));
            
            Assert.Equal("Invalid password.", exception.Message);
        }

        [Fact]
        public async Task EnsureUserCanLoginAsync_ShouldNotThrow_WhenUserIsActiveAndNotBlocked()
        {
            var user = CreateTestUser(isActive: true, isBlocked: false);

            var exception = await Record.ExceptionAsync(() => _service.EnsureUserCanLoginAsync(user));

            Assert.Null(exception);
        }

        [Fact]
        public async Task EnsureUserCanLoginAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotActive()
        {
            var user = CreateTestUser(isActive: false, isBlocked: false);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _service.EnsureUserCanLoginAsync(user));
            
            Assert.Equal("Please activate your account by following the link sent to your email.", exception.Message);
        }

        [Fact]
        public async Task EnsureUserCanLoginAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsBlocked()
        {
            var user = CreateTestUser(isActive: true, isBlocked: true);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _service.EnsureUserCanLoginAsync(user));
            
            Assert.Equal("User is banned.", exception.Message);
        }

        [Fact]
        public async Task IssueLoginTokenAsync_ShouldReturnTokenWithLongExpiration_WhenRememberIsTrue()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = "UA", Platform = "Win" };
            var ip = "127.0.0.1";

            _tokenGeneratorMock.Setup(x => x.GenerateJwtToken(user, device)).Returns("access_token");
            _tokenGeneratorMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            var result = await _service.IssueLoginTokenAsync(user, device, ip, remember: true);

            Assert.NotNull(result);
            Assert.Equal("access_token", result.Token);
            Assert.Equal("refresh_token", result.RefreshToken);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(device, result.DeviceInfo);
            Assert.Equal(ip, result.IpAddress);
            Assert.True(result.IsActive);
            Assert.True(result.Expiration > DateTime.UtcNow);
            Assert.True(result.RefreshTokenExpiration > DateTime.UtcNow.AddDays(27));
        }

        [Fact]
        public async Task IssueLoginTokenAsync_ShouldReturnTokenWithShortExpiration_WhenRememberIsFalse()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = "UA", Platform = "Win" };
            var ip = "127.0.0.1";

            _tokenGeneratorMock.Setup(x => x.GenerateJwtToken(user, device)).Returns("access_token");
            _tokenGeneratorMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            var result = await _service.IssueLoginTokenAsync(user, device, ip, remember: false);

            Assert.True(result.RefreshTokenExpiration < DateTime.UtcNow.AddHours(7));
        }

        [Fact]
        public async Task CleanupUserSessionsAsync_ShouldInvalidateExpiredAndDuplicateAndCurrentDeviceTokens()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = "UA", Platform = "Win" };
            var ip = "127.0.0.1";

            var expiredToken = new LoginToken 
            { 
                Token = "t1", 
                RefreshToken = "r1",
                IsActive = true, 
                Expiration = DateTime.UtcNow.AddMinutes(-5),
                DeviceInfo = device,
                IpAddress = ip
            };
            
            var duplicateOldToken = new LoginToken 
            { 
                Token = "t2", 
                RefreshToken = "r2",
                IsActive = true, 
                Expiration = DateTime.UtcNow.AddMinutes(10),
                DeviceInfo = new DeviceInfo { UserAgent = "Other", Platform = "Linux" },
                IpAddress = "8.8.8.8"
            };
            var duplicateNewToken = new LoginToken 
            { 
                Token = "t3", 
                RefreshToken = "r3",
                IsActive = true, 
                Expiration = DateTime.UtcNow.AddMinutes(20),
                DeviceInfo = new DeviceInfo { UserAgent = "Other", Platform = "Linux" },
                IpAddress = "8.8.8.8"
            };

            var currentDeviceToken = new LoginToken 
            { 
                Token = "t4", 
                RefreshToken = "r4",
                IsActive = true, 
                Expiration = DateTime.UtcNow.AddMinutes(15),
                DeviceInfo = device,
                IpAddress = ip
            };

            user.LoginTokens!.Add(expiredToken);
            user.LoginTokens.Add(duplicateOldToken);
            user.LoginTokens.Add(duplicateNewToken);
            user.LoginTokens.Add(currentDeviceToken);

            await _service.CleanupUserSessionsAsync(user, device, ip);

            Assert.False(expiredToken.IsActive);
            Assert.False(duplicateOldToken.IsActive);
            Assert.True(duplicateNewToken.IsActive);
            Assert.False(currentDeviceToken.IsActive);

            _userRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ApplySessionLimitsAsync_ShouldDeactivateOldestToken_WhenActiveTokensCountReachesLimit()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = "UA", Platform = "Win" };
            
            var oldestToken = new LoginToken { Token = "oldest", RefreshToken = "r1", DeviceInfo = device, IpAddress = "127.0.0.1", IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(5) };
            var token2 = new LoginToken { Token = "t2", RefreshToken = "r2", DeviceInfo = device, IpAddress = "127.0.0.1", IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(10) };
            var token3 = new LoginToken { Token = "t3", RefreshToken = "r3", DeviceInfo = device, IpAddress = "127.0.0.1", IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(15) };
            var token4 = new LoginToken { Token = "t4", RefreshToken = "r4", DeviceInfo = device, IpAddress = "127.0.0.1", IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(20) };
            var token5 = new LoginToken { Token = "t5", RefreshToken = "r5", DeviceInfo = device, IpAddress = "127.0.0.1", IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(25) };

            user.LoginTokens!.Add(oldestToken);
            user.LoginTokens.Add(token2);
            user.LoginTokens.Add(token3);
            user.LoginTokens.Add(token4);
            user.LoginTokens.Add(token5);

            await _service.ApplySessionLimitsAsync(user);

            Assert.False(oldestToken.IsActive);
            Assert.True(token2.IsActive);
            Assert.True(token3.IsActive);
            Assert.True(token4.IsActive);
            Assert.True(token5.IsActive);
        }

        [Fact]
        public async Task ApplySessionLimitsAsync_ShouldDoNothing_WhenActiveTokensCountIsLessThanLimit()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = "UA", Platform = "Win" };
            var token = new LoginToken { Token = "t", RefreshToken = "r", DeviceInfo = device, IpAddress = "127.0.0.1", IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(10) };
            user.LoginTokens!.Add(token);

            await _service.ApplySessionLimitsAsync(user);

            Assert.True(token.IsActive);
        }
    }
}