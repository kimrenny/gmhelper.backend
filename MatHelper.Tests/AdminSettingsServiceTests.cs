using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatHelper.Tests.BLL
{
    public class AdminSettingsServiceTests
    {
        private readonly Mock<IAdminSettingsRepository> _adminSettingsRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ILogger<AdminSettingsService>> _loggerMock;

        public AdminSettingsServiceTests()
        {
            _adminSettingsRepoMock = new Mock<IAdminSettingsRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<AdminSettingsService>>();
        }

        private AdminSettingsService CreateService()
        {
            return new AdminSettingsService(
                _adminSettingsRepoMock.Object,
                _userRepoMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetOrCreateAdminSettingsAsync_ShouldReturnExistingSettings_WhenTheyExist()
        {
            var userId = Guid.NewGuid();

            var adminSettings = new AdminSettings
            {
                UserId = userId,
                User = new User
                {
                    Id = userId,
                    Username = "testuser",
                    Email = "test@example.com",
                    RegistrationDate = DateTime.UtcNow,
                    PasswordHash = "hash",
                    PasswordSalt = "salt",
                    Role = "Admin",
                    IsBlocked = false,
                    IsActive = true
                },
                Sections = new List<AdminSection>
                {
                    new AdminSection
                    {
                        Id = 1,
                        Title = "Dashboard",
                        AdminSettings = null!,
                        Switches = new List<AdminSwitch>()
                    }
                }
            };

            foreach (var section in adminSettings.Sections)
            {
                section.AdminSettings = adminSettings;
                section.Switches.Add(new AdminSwitch
                {
                    Id = 1,
                    Label = "requests",
                    Value = true,
                    AdminSection = section
                });
            }

            _adminSettingsRepoMock
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(adminSettings);

            var service = CreateService();
            var result = await service.GetOrCreateAdminSettingsAsync(userId);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Single(result[0]);
            Assert.True(result[0][0]);
        }

        [Fact]
        public async Task GetOrCreateAdminSettingsAsync_ShouldCreateSettings_WhenNoneExist()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "newuser",
                Email = "new@example.com",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "Admin",
                IsBlocked = false,
                IsActive = true
            };

            _adminSettingsRepoMock
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync((AdminSettings?)null);

            _userRepoMock
                .Setup(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            _adminSettingsRepoMock
                .Setup(r => r.CreateAsync(It.IsAny<AdminSettings>()))
                .Returns(Task.CompletedTask);

            var service = CreateService();
            var result = await service.GetOrCreateAdminSettingsAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(4, result.Length);
            foreach (var section in result)
            {
                Assert.Equal(5, section.Length);
                Assert.All(section, val => Assert.True(val));
            }

            _adminSettingsRepoMock.Verify(r => r.CreateAsync(It.IsAny<AdminSettings>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSwitchAsync_ShouldReturnRepositoryResult()
        {
            var userId = Guid.NewGuid();
            _adminSettingsRepoMock
                .Setup(r => r.UpdateSwitchAsync(userId, "dashboard", "requests", false))
                .ReturnsAsync(true);

            var service = CreateService();
            var result = await service.UpdateSwitchAsync(userId, "Dashboard", "requests", false);

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateSwitchAsync_ShouldReturnFalse_WhenExceptionThrown()
        {
            var userId = Guid.NewGuid();
            _adminSettingsRepoMock
                .Setup(r => r.UpdateSwitchAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(new Exception("test exception"));

            var service = CreateService();
            var result = await service.UpdateSwitchAsync(userId, "Dashboard", "requests", true);

            Assert.False(result);
        }
    }
}
