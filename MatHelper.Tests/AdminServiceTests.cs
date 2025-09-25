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
        private readonly Mock<IUserMapper> _userMapperMock;
        private readonly Mock<ILogger<AdminService>> _loggerMock;

        public AdminServiceTests()
        {
            _securityServiceMock = new Mock<ISecurityService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _loginTokenRepositoryMock = new Mock<ILoginTokenRepository>();
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
                _userMapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetUsersAsync_ShouldReturnMappedUsers()
        {
            var userEntities = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "test",
                    Role = "User",
                    Email = "test@example.com",
                    PasswordHash = "hash",
                    PasswordSalt = "salt",
                    RegistrationDate = DateTime.UtcNow
                }
            };

            var mappedDtos = new List<AdminUserDto>
            {
                new AdminUserDto
                {
                    Id = userEntities[0].Id,
                    Username = "test",
                    Role = "User",
                    Email = "test@example.com"
                }
            };


            _userRepositoryMock.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(userEntities);
            _userMapperMock.Setup(m => m.MapToAdminUserDto(It.IsAny<User>())).Returns((User u) => mappedDtos.First(d => d.Id == u.Id));

            var service = CreateService();

            var result = await service.GetUsersAsync();

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("test", result[0].Username);
        }

        [Fact]
        public async Task GetUsersAsync_ShouldThrow_WhenNoUsers()
        {
            _userRepositoryMock.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(new List<User>());
            var service = CreateService();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync());
        }

        [Fact]
        public async Task ActionUserAsync_ShouldCallRepository_WithCorrectParameters()
        {
            var userId = Guid.NewGuid();
            var action = "Ban";

            _userRepositoryMock.Setup(r => r.ActionUserAsync(userId, It.IsAny<CORE.Enums.UserAction>())).Returns(Task.CompletedTask);

            var service = CreateService();

            await service.ActionUserAsync(userId, action);

            _userRepositoryMock.Verify(r => r.ActionUserAsync(userId, CORE.Enums.UserAction.Ban), Times.Once);
        }

        [Fact]
        public async Task GetTokensAsync_ShouldReturnTokens()
        {
            var tokens = new List<TokenDto>
            {
                new TokenDto
                {
                    Token = "abc",
                    IpAddress = "127.0.0.1",
                    DeviceInfo = new DeviceInfo { Platform = "Windows", UserAgent = "TestAgent" }
                }
            };

            _userRepositoryMock.Setup(r => r.GetAllTokensAsync()).ReturnsAsync(tokens);

            var service = CreateService();

            var result = await service.GetTokensAsync();

            Assert.Single(result);
            Assert.Equal("abc", result[0].Token);
        }

    }

}