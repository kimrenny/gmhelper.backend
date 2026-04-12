using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.BLL.Interfaces;

namespace MatHelper.Tests.BLL
{
    public class AdminServiceTests
    {
        private readonly Mock<ISecurityService> _securityServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ILoginTokenRepository> _loginTokenRepositoryMock;
        private readonly Mock<INotFoundReportRepository> _notFoundReportRepositoryMock;
        private readonly Mock<IUserMapper> _userMapperMock;
        private readonly Mock<ILogger<AdminService>> _loggerMock;

        public AdminServiceTests()
        {
            _securityServiceMock = new Mock<ISecurityService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _loginTokenRepositoryMock = new Mock<ILoginTokenRepository>();
            _notFoundReportRepositoryMock = new Mock<INotFoundReportRepository>();
            _userMapperMock = new Mock<IUserMapper>();
            _loggerMock = new Mock<ILogger<AdminService>>();
        }

        private AdminService CreateService()
        {
            return new AdminService(
                _securityServiceMock.Object,
                _tokenServiceMock.Object,
                _userRepositoryMock.Object,
                _loginTokenRepositoryMock.Object,
                _notFoundReportRepositoryMock.Object,
                _userMapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetUsersAsync_ShouldReturnMappedUsers()
        {
            var userId = Guid.NewGuid();

            var users = new List<User>
            {
                new User
                {
                    Id = userId,
                    Username = "test",
                    Role = "User",
                    Email = "test@example.com",
                    PasswordHash = "hash",
                    PasswordSalt = "salt",
                    RegistrationDate = DateTime.UtcNow
                }
            };

            var asyncUsers = new TestAsyncEnumerable<User>(users);

            _userRepositoryMock
                .Setup(r => r.GetUsersQuery())
                .Returns(asyncUsers);

            _userMapperMock
                .Setup(m => m.MapToAdminUserDto(It.IsAny<User>()))
                .Returns((User u) => new AdminUserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    Email = u.Email
                });

            var service = CreateService();

            var result = await service.GetUsersAsync(1, 10, "RegistrationDate", false, null);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("test", result.Items[0].Username);
        }

        [Fact]
        public async Task GetUsersAsync_ShouldThrow_WhenNoUsers()
        {
            var asyncUsers = new TestAsyncEnumerable<User>(new List<User>());

            _userRepositoryMock
                .Setup(r => r.GetUsersQuery())
                .Returns(asyncUsers);

            var service = CreateService();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetUsersAsync(1, 10, "RegistrationDate", false, null));
        }

        [Fact]
        public async Task ActionUserAsync_ShouldCallRepository_WithCorrectParameters()
        {
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(r => r.ActionUserAsync(userId, CORE.Enums.UserAction.Ban))
                .Returns(Task.CompletedTask);

            var service = CreateService();

            await service.ActionUserAsync(userId, "Ban");

            _userRepositoryMock
                .Verify(r => r.ActionUserAsync(userId, CORE.Enums.UserAction.Ban), Times.Once);
        }

        [Fact]
        public async Task GetTokensAsync_ShouldReturnTokens()
        {
            var tokens = new List<LoginToken>
            {
                new LoginToken
                {
                    Token = "abc",
                    IpAddress = "127.0.0.1",
                    DeviceInfo = new DeviceInfo
                    {
                        Platform = "Windows",
                        UserAgent = "TestAgent"
                    },
                    Expiration = DateTime.UtcNow.AddDays(1),
                    RefreshToken = "refresh",
                    RefreshTokenExpiration = DateTime.UtcNow.AddDays(2),
                    UserId = Guid.NewGuid(),
                    IsActive = true
                }
            };

            var asyncTokens = new TestAsyncEnumerable<LoginToken>(tokens);

            _userRepositoryMock
                .Setup(r => r.GetTokensQuery())
                .Returns(asyncTokens);

            var service = CreateService();

            var result = await service.GetTokensAsync(1, 10, "Expiration", false, null);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("abc", result.Items[0].Token);
        }
    }

}