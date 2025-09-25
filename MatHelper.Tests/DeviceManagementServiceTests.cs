using MatHelper.BLL.Services;
using MatHelper.DAL.Interfaces;
using MatHelper.CORE.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MatHelper.Tests
{
    public class DeviceManagementServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<ILogger<DeviceManagementService>> _loggerMock = new();
        private readonly DeviceManagementService _service;

        public DeviceManagementServiceTests()
        {
            _service = new DeviceManagementService(_userRepoMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetLoggedDevicesAsync_ShouldThrow_WhenUserNotFound()
        {
            _userRepoMock.Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);

            await Assert.ThrowsAsync<Exception>(() => _service.GetLoggedDevicesAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetLoggedDevicesAsync_ShouldReturnEmpty_WhenNoActiveTokens()
        {
            var user = new User 
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "User",
                LoginTokens = new List<LoginToken>()
            };
            _userRepoMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _service.GetLoggedDevicesAsync(user.Id);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetLoggedDevicesAsync_ShouldReturnActiveTokens()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "User",
                LoginTokens = new List<LoginToken>
                {
                    new LoginToken
                    {
                        Token = "token1",
                        RefreshToken = "refresh1",
                        Expiration = DateTime.UtcNow.AddHours(1),
                        RefreshTokenExpiration = DateTime.UtcNow.AddHours(2),
                        UserId = Guid.NewGuid(),
                        DeviceInfo = new DeviceInfo { Platform = "Win", UserAgent = "UA" }, 
                        IpAddress = "127.0.0.1",
                        IsActive = true
                    },
                    new LoginToken
                    {
                        Token = "token2",
                        RefreshToken = "refresh2",
                        Expiration = DateTime.UtcNow.AddHours(1),
                        RefreshTokenExpiration = DateTime.UtcNow.AddHours(2),
                        UserId = Guid.NewGuid(),
                        DeviceInfo = new DeviceInfo { Platform = "Mac", UserAgent = "UA2" },
                        IpAddress = "192.168.0.1",
                        IsActive = true
                    }
                }
            };

            _userRepoMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _service.GetLoggedDevicesAsync(user.Id);

            var devices = result.ToList();
            Assert.Equal(2, devices.Count);

            var device1 = devices.First(d => d.GetType().GetProperty("Platform")!.GetValue(d)!.ToString() == "Win");
            Assert.Equal("UA", device1.GetType().GetProperty("UserAgent")!.GetValue(device1));
            Assert.Equal("127.0.0.1", device1.GetType().GetProperty("IpAddress")!.GetValue(device1));

            var device2 = devices.First(d => d.GetType().GetProperty("Platform")!.GetValue(d)!.ToString() == "Mac");
            Assert.Equal("UA2", device2.GetType().GetProperty("UserAgent")!.GetValue(device2));
            Assert.Equal("192.168.0.1", device2.GetType().GetProperty("IpAddress")!.GetValue(device2));

        }

        [Fact]
        public async Task RemoveDeviceAsync_ShouldReturnUserNotFound_WhenUserMissing()
        {
            _userRepoMock.Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null!);

            var result = await _service.RemoveDeviceAsync(Guid.NewGuid(), "UA", "Win", "127.0.0.1", "token");

            Assert.Equal("User not found.", result);
        }

        [Fact]
        public async Task RemoveDeviceAsync_ShouldReturnDeviceNotFound_WhenTokenMissing()
        {
            var user = new User 
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "User",
                LoginTokens = new List<LoginToken>()
            };
            _userRepoMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _service.RemoveDeviceAsync(user.Id, "UA", "Win", "127.0.0.1", "token");

            Assert.Equal("Device not found or inactive.", result);
        }

        [Fact]
        public async Task RemoveDeviceAsync_ShouldReturnCannotDeactivateCurrent_WhenTokenMatches()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "User",
                LoginTokens = new List<LoginToken>
                {
                    new LoginToken 
                    { 
                        Token = "token",
                        RefreshToken = "refresh",
                        Expiration = DateTime.UtcNow.AddHours(1),
                        RefreshTokenExpiration = DateTime.UtcNow.AddHours(2),
                        UserId = Guid.NewGuid(),
                        DeviceInfo = new DeviceInfo { Platform = "Win", UserAgent = "UA" }, 
                        IpAddress = "127.0.0.1",
                        IsActive = true
                    }
                }
            };
            _userRepoMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);

            var result = await _service.RemoveDeviceAsync(user.Id, "UA", "Win", "127.0.0.1", "token");

            Assert.Equal("The current device cannot be deactivated.", result);
        }

        [Fact]
        public async Task RemoveDeviceAsync_ShouldDeactivateDevice_WhenValid()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "User",
                LoginTokens = new List<LoginToken>
                {
                    new LoginToken
                    {
                        Token = "token1",
                        RefreshToken = "refresh1",
                        Expiration = DateTime.UtcNow.AddHours(1),
                        RefreshTokenExpiration = DateTime.UtcNow.AddHours(2),
                        UserId = Guid.NewGuid(),
                        DeviceInfo = new DeviceInfo { Platform = "Win", UserAgent = "UA" },
                        IpAddress = "127.0.0.1",
                        IsActive = true
                    }
                }
            };
            _userRepoMock.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
            _userRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.RemoveDeviceAsync(user.Id, "UA", "Win", "127.0.0.1", "other-token2");

            Assert.Equal("Device removed successfully.", result);
            Assert.False(user.LoginTokens.First().IsActive);
            _userRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
    }
}
