using MatHelper.BLL.Services;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MatHelper.Tests.Services
{
    public class TokenServiceTests
    {
        private readonly Mock<ISecurityService> _securityMock;
        private readonly Mock<ITokenGeneratorService> _tokenGenMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ILoginTokenRepository> _loginTokenRepoMock;
        private readonly Mock<ILogger<TokenService>> _loggerMock;
        private readonly TokenService _service;

        public TokenServiceTests()
        {
            _securityMock = new Mock<ISecurityService>();
            _tokenGenMock = new Mock<ITokenGeneratorService>();
            _userRepoMock = new Mock<IUserRepository>();
            _loginTokenRepoMock = new Mock<ILoginTokenRepository>();
            _loggerMock = new Mock<ILogger<TokenService>>();

            _service = new TokenService(
                _securityMock.Object,
                _tokenGenMock.Object,
                _userRepoMock.Object,
                _loginTokenRepoMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_ReturnsNewTokens_WhenValid()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "TestUser",
                Email = "test@example.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "Admin",
                RegistrationDate = DateTime.UtcNow,
                IsBlocked = false,
                LoginTokens = new List<LoginToken>()
            };

            var deviceInfo = new DeviceInfo
            {
                UserAgent = "UA",
                Platform = "Windows"
            };

            var loginToken = new LoginToken
            {
                Token = "oldAccess",
                RefreshToken = "refreshToken",
                RefreshTokenExpiration = DateTime.UtcNow.AddMinutes(10),
                Expiration = DateTime.UtcNow.AddMinutes(10),
                IsActive = true,
                User = user,
                UserId = userId,
                DeviceInfo = deviceInfo,
                IpAddress = "127.0.0.1"
            };

            user.LoginTokens.Add(loginToken);

            _loginTokenRepoMock.Setup(r => r.GetLoginTokenByRefreshTokenAsync("refreshToken"))
                .ReturnsAsync(loginToken);

            _tokenGenMock.Setup(t => t.GenerateJwtToken(user, deviceInfo))
                .Returns("newAccess");

            _tokenGenMock.Setup(t => t.GenerateRefreshToken())
                .Returns("newRefresh");

            _userRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.RefreshAccessTokenAsync("refreshToken");

            Assert.Equal("newAccess", result.AccessToken);
            Assert.Equal("newRefresh", result.RefreshToken);
            Assert.Equal("newAccess", loginToken.Token);
            Assert.Equal("newRefresh", loginToken.RefreshToken);
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_Throws_WhenTokenInvalid()
        {
            _loginTokenRepoMock.Setup(r => r.GetLoginTokenByRefreshTokenAsync("bad"))
                .ReturnsAsync((LoginToken?)null);

            await Assert.ThrowsAsync<Exception>(async () =>
                await _service.RefreshAccessTokenAsync("bad")
            );
        }

        [Fact]
        public async Task IsTokenDisabled_ReturnsTrue_WhenExpiredOrInactive()
        {
            var loginToken = new LoginToken
            {
                Token = "t",
                RefreshToken = "r",
                Expiration = DateTime.UtcNow.AddMinutes(-1),
                IsActive = true,
                DeviceInfo = new DeviceInfo(),
                IpAddress = "127.0.0.1"
                
            };

            _loginTokenRepoMock.Setup(r => r.GetLoginTokenAsync("t"))
                .ReturnsAsync(loginToken);

            var result = await _service.IsTokenDisabled("t");
            Assert.True(result);

            loginToken.Expiration = DateTime.UtcNow.AddMinutes(10);
            loginToken.IsActive = false;
            result = await _service.IsTokenDisabled("t");
            Assert.True(result);
        }

        [Fact]
        public async Task IsTokenDisabled_ReturnsFalse_WhenActive()
        {
            var loginToken = new LoginToken
            {
                Token = "t",
                RefreshToken = "r",
                Expiration = DateTime.UtcNow.AddMinutes(10),
                IsActive = true,
                DeviceInfo = new DeviceInfo(),
                IpAddress = "127.0.0.1"
            };

            _loginTokenRepoMock.Setup(r => r.GetLoginTokenAsync("t"))
                .ReturnsAsync(loginToken);

            var result = await _service.IsTokenDisabled("t");
            Assert.False(result);
        }

        [Fact]
        public void ExtractTokenAsync_ReturnsToken_WhenHeaderValid()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer myToken";

            var token = _service.ExtractTokenAsync(context.Request);
            Assert.Equal("myToken", token);
        }

        [Fact]
        public void ExtractTokenAsync_ReturnsNull_WhenHeaderInvalid()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "InvalidHeader";

            var token = _service.ExtractTokenAsync(context.Request);
            Assert.Null(token);
        }

        [Fact]
        public async Task GetUserIdFromTokenAsync_ReturnsGuid()
        {
            var userId = Guid.NewGuid();
            _loginTokenRepoMock.Setup(r => r.GetUserIdByAuthTokenAsync("token"))
                .ReturnsAsync(userId);

            var result = await _service.GetUserIdFromTokenAsync("token");
            Assert.Equal(userId, result);
        }

        [Fact]
        public async Task HasAdminPermissionsAsync_DelegatesToSecurityService()
        {
            var userId = Guid.NewGuid();
            _securityMock.Setup(s => s.HasAdminPermissionsAsync(userId))
                .ReturnsAsync(true);

            var result = await _service.HasAdminPermissionsAsync(userId);
            Assert.True(result);
        }
    }
}
