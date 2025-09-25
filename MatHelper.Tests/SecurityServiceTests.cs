using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace MatHelper.Tests.Services
{
    public class SecurityServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ILoginTokenRepository> _loginTokenRepoMock;
        private readonly Mock<ILogger<SecurityService>> _loggerMock;
        private readonly SecurityService _service;

        public SecurityServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _loginTokenRepoMock = new Mock<ILoginTokenRepository>();
            _loggerMock = new Mock<ILogger<SecurityService>>();

            _service = new SecurityService(
                _userRepoMock.Object,
                _loginTokenRepoMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public void HashPassword_Throws_WhenPasswordIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => _service.HashPassword("", "salt"));
        }

        [Fact]
        public void HashPassword_Throws_WhenSaltIsEmpty()
        {
            Assert.Throws<ArgumentException>(() => _service.HashPassword("password", ""));
        }

        [Fact]
        public void HashPassword_ProducesConsistentHash()
        {
            var salt = Convert.ToBase64String(Encoding.UTF8.GetBytes("mysalt"));
            var hash1 = _service.HashPassword("mypassword", salt);
            var hash2 = _service.HashPassword("mypassword", salt);

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void VerifyPassword_ReturnsTrue_WhenCorrect()
        {
            var salt = Convert.ToBase64String(Encoding.UTF8.GetBytes("mysalt"));
            var hash = _service.HashPassword("mypassword", salt);

            var result = _service.VerifyPassword("mypassword", hash, salt);

            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_WhenIncorrect()
        {
            var salt = Convert.ToBase64String(Encoding.UTF8.GetBytes("mysalt"));
            var hash = _service.HashPassword("mypassword", salt);

            var result = _service.VerifyPassword("wrongpass", hash, salt);

            Assert.False(result);
        }

        [Fact]
        public void GenerateSalt_ReturnsUniqueValues()
        {
            var salt1 = _service.GenerateSalt();
            var salt2 = _service.GenerateSalt();

            Assert.NotEqual(salt1, salt2);
            Assert.False(string.IsNullOrWhiteSpace(salt1));
            Assert.False(string.IsNullOrWhiteSpace(salt2));
        }

        [Fact]
        public async Task CheckSuspiciousActivityAsync_BlocksUsers_WhenMoreThan3()
        {
            var ip = "1.1.1.1";
            var userAgent = "TestAgent";
            var platform = "Windows";

            var tokens = new List<LoginToken>
            {
                new LoginToken { UserId = Guid.NewGuid(), IpAddress = ip, DeviceInfo = new DeviceInfo { UserAgent = userAgent, Platform = platform }, Token = "token", RefreshToken = "refresh" },
                new LoginToken { UserId = Guid.NewGuid(), IpAddress = ip, DeviceInfo = new DeviceInfo { UserAgent = userAgent, Platform = platform }, Token = "token", RefreshToken = "refresh" },
                new LoginToken { UserId = Guid.NewGuid(), IpAddress = ip, DeviceInfo = new DeviceInfo { UserAgent = userAgent, Platform = platform }, Token = "token", RefreshToken = "refresh" },
                new LoginToken { UserId = Guid.NewGuid(), IpAddress = ip, DeviceInfo = new DeviceInfo { UserAgent = userAgent, Platform = platform }, Token = "token", RefreshToken = "refresh" }
            };

            _loginTokenRepoMock.Setup(r => r.GetAllLoginTokensAsync()).ReturnsAsync(tokens);

            foreach (var token in tokens)
            {
                _userRepoMock.Setup(r => r.GetUserByIdAsync(token.UserId))
                    .ReturnsAsync(new User 
                        { 
                            Id = token.UserId, 
                            Username = "TestUser",
                            Role = "User",
                            RegistrationDate = DateTime.UtcNow,
                            PasswordHash = "hash",
                            PasswordSalt = "salt",
                            Email = "test@example.com",
                            IsBlocked = false, 
                            LoginTokens = new List<LoginToken> { token } 
                        }
                    );
            }

            _userRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.CheckSuspiciousActivityAsync(ip, userAgent, platform);

            Assert.True(result);
            foreach (var token in tokens)
            {
                var user = await _userRepoMock.Object.GetUserByIdAsync(token.UserId);
                Assert.True(user?.IsBlocked);
            }
        }


        [Fact]
        public async Task CheckSuspiciousActivityAsync_ReturnsFalse_WhenLessThanThreshold()
        {
            var tokens = new List<LoginToken>
            {
                new LoginToken
                {
                    Token = "token",
                    RefreshToken = "refresh",
                    UserId = Guid.NewGuid(),
                    IpAddress = "1.1.1.1",
                    DeviceInfo = new DeviceInfo { UserAgent = "UA", Platform = "Win" }
                }
            };

            _loginTokenRepoMock.Setup(r => r.GetAllLoginTokensAsync()).ReturnsAsync(tokens);

            var result = await _service.CheckSuspiciousActivityAsync("1.1.1.1", "UA", "Win");

            Assert.False(result);
        }

        [Fact]
        public async Task HasAdminPermissionsAsync_ReturnsTrue_ForAdmin()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Role = "Admin",
                Username = "test",
                Email = "a@a.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                RegistrationDate = DateTime.UtcNow
            };

            _userRepoMock.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _service.HasAdminPermissionsAsync(user.Id);

            Assert.True(result);
        }

        [Fact]
        public async Task HasAdminPermissionsAsync_ReturnsFalse_WhenNotAdmin()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Role = "User",
                Username = "test",
                Email = "a@a.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                RegistrationDate = DateTime.UtcNow
            };

            _userRepoMock.Setup(r => r.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _service.HasAdminPermissionsAsync(user.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCountryByIpAsync_ReturnsLocalhost_For127()
        {
            var result = await _service.GetCountryByIpAsync("127.0.0.1");

            Assert.Equal("localhost", result);
        }

        [Fact]
        public async Task GetCountryByIpAsync_ReturnsUnknown_OnHttpError()
        {
            var result = await _service.GetCountryByIpAsync("256.256.256.256");

            Assert.Equal("Unknown", result);
        }
    }
}
