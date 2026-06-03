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
    public class SecurityPolicyServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<ILogger<SecurityPolicyService>> _loggerMock = new();

        private readonly SecurityPolicyService _service;

        public SecurityPolicyServiceTests()
        {
            _service = new SecurityPolicyService(
                _userRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        private User CreateTestUser(string role = "User") => new()
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@example.com",
            Username = $"user_{Guid.NewGuid()}",
            PasswordHash = "hash",
            Role = role,
            RegistrationDate = DateTime.UtcNow,
            IsBlocked = false,
            LoginTokens = new List<LoginToken>()
        };

        [Fact]
        public async Task EnforceRegistrationIpLimitAsync_ShouldReturn_WhenUserCountIsLessThanLimit()
        {
            var ip = "192.168.1.1";
            _userRepositoryMock.Setup(x => x.GetUserCountByIpAsync(ip)).ReturnsAsync(2);

            await _service.EnforceRegistrationIpLimitAsync(ip);

            _userRepositoryMock.Verify(x => x.GetUsersByIpAsync(It.IsAny<string>()), Times.Never);
            _userRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task EnforceRegistrationIpLimitAsync_ShouldThrowInvalidOperationException_WhenLimitExceededButNoUsersFound()
        {
            var ip = "192.168.1.1";
            _userRepositoryMock.Setup(x => x.GetUserCountByIpAsync(ip)).ReturnsAsync(3);
            _userRepositoryMock.Setup(x => x.GetUsersByIpAsync(ip)).ReturnsAsync(new List<User>());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.EnforceRegistrationIpLimitAsync(ip));

            Assert.Equal("No users found with the specified IP address.", exception.Message);
            _userRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task EnforceRegistrationIpLimitAsync_ShouldBlockUsersAndDeactivateTokens_WhenLimitExceeded()
        {
            var ip = "192.168.1.1";
            var device = new DeviceInfo { UserAgent = "UA", Platform = "Win" };
            
            var user1 = CreateTestUser(role: "User");
            var token1 = new LoginToken { Token = "t1", RefreshToken = "r1", DeviceInfo = device, IpAddress = ip, IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(10) };
            user1.LoginTokens!.Add(token1);

            var user2 = CreateTestUser(role: "Admin");
            var token2 = new LoginToken { Token = "t2", RefreshToken = "r2", DeviceInfo = device, IpAddress = ip, IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(10) };
            user2.LoginTokens!.Add(token2);

            var ownerUser = CreateTestUser(role: "Owner");
            var token3 = new LoginToken { Token = "t3", RefreshToken = "r3", DeviceInfo = device, IpAddress = ip, IsActive = true, Expiration = DateTime.UtcNow.AddMinutes(10) };
            ownerUser.LoginTokens!.Add(token3);

            var usersList = new List<User> { user1, user2, ownerUser };

            _userRepositoryMock.Setup(x => x.GetUserCountByIpAsync(ip)).ReturnsAsync(3);
            _userRepositoryMock.Setup(x => x.GetUsersByIpAsync(ip)).ReturnsAsync(usersList);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _service.EnforceRegistrationIpLimitAsync(ip));

            Assert.Equal("Violation of service rules. All user accounts have been blocked.", exception.Message);
            
            Assert.True(user1.IsBlocked);
            Assert.False(token1.IsActive);

            Assert.True(user2.IsBlocked);
            Assert.False(token2.IsActive);

            Assert.False(ownerUser.IsBlocked);
            Assert.True(token3.IsActive);

            _userRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public void ValidateDeviceInfo_ShouldNotThrow_WhenDeviceInfoIsValid()
        {
            var device = new DeviceInfo { UserAgent = "Mozilla", Platform = "Windows" };

            var exception = Record.Exception(() => _service.ValidateDeviceInfo(device));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(null, "Windows")]
        [InlineData("Mozilla", null)]
        [InlineData(null, null)]
        public void ValidateDeviceInfo_ShouldThrowInvalidDataException_WhenPropertiesAreNull(string? userAgent, string? platform)
        {
            var device = new DeviceInfo { UserAgent = userAgent, Platform = platform };

            var exception = Assert.Throws<InvalidDataException>(() => _service.ValidateDeviceInfo(device));

            Assert.Equal("deviceInfo does not meet the requirements", exception.Message);
        }

        [Fact]
        public void IsUnfamiliar_ShouldReturnTrue_WhenLastTokenIsNull()
        {
            var result = _service.IsUnfamiliar(null, "127.0.0.1", "Mozilla");

            Assert.True(result);
        }

        [Fact]
        public void IsUnfamiliar_ShouldReturnTrue_WhenIpAddressDoesNotMatch()
        {
            var device = new DeviceInfo { UserAgent = "Mozilla", Platform = "Windows" };
            var token = new LoginToken { Token = "t", RefreshToken = "r", DeviceInfo = device, IpAddress = "192.168.1.1" };

            var result = _service.IsUnfamiliar(token, "127.0.0.1", "Mozilla");

            Assert.True(result);
        }

        [Fact]
        public void IsUnfamiliar_ShouldReturnTrue_WhenUserAgentDoesNotMatch()
        {
            var device = new DeviceInfo { UserAgent = "Mozilla", Platform = "Windows" };
            var token = new LoginToken { Token = "t", RefreshToken = "r", DeviceInfo = device, IpAddress = "127.0.0.1" };

            var result = _service.IsUnfamiliar(token, "127.0.0.1", "OtherAgent");

            Assert.True(result);
        }

        [Fact]
        public void IsUnfamiliar_ShouldReturnFalse_WhenIpAndUserAgentMatch()
        {
            var device = new DeviceInfo { UserAgent = "Mozilla", Platform = "Windows" };
            var token = new LoginToken { Token = "t", RefreshToken = "r", DeviceInfo = device, IpAddress = "127.0.0.1" };

            var result = _service.IsUnfamiliar(token, "127.0.0.1", "Mozilla");

            Assert.False(result);
        }
    }
}