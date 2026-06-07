using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Moq;
using Xunit;

namespace MatHelper.Tests
{
    public class RegistrationServiceTests
    {
        private readonly Mock<ITokenGeneratorService> _tokenGeneratorServiceMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();

        private readonly RegistrationService _service;

        public RegistrationServiceTests()
        {
            _service = new RegistrationService(
                _tokenGeneratorServiceMock.Object,
                _userRepositoryMock.Object
            );
        }

        private User CreateTestUser(string email = "test@example.com", string username = "testuser", bool isActive = true) => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            PasswordHash = "hash",
            Role = "User",
            RegistrationDate = DateTime.UtcNow,
            IsActive = isActive,
            LoginTokens = new List<LoginToken>()
        };

        [Fact]
        public async Task EnsureEmailAndUsernameUniqueAsync_ShouldNotThrow_WhenEmailAndUsernameAreUnique()
        {
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null!);

            _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null!);

            var exception = await Record.ExceptionAsync(() =>
                _service.EnsureEmailAndUsernameUniqueAsync("new@example.com", "newuser"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task EnsureEmailAndUsernameUniqueAsync_ShouldThrow_WhenEmailIsUsed()
        {
            var existingUser = CreateTestUser();

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.EnsureEmailAndUsernameUniqueAsync("email", "user"));

            Assert.Equal("Email is already used by another user.", ex.Message);
        }

        [Fact]
        public async Task EnsureEmailAndUsernameUniqueAsync_ShouldThrow_WhenUsernameIsUsed()
        {
            var existingUser = CreateTestUser();

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null!);

            _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.EnsureEmailAndUsernameUniqueAsync("email", "user"));

            Assert.Equal("Username is already used by another user.", ex.Message);
        }

        [Fact]
        public async Task BuildNewUserAsync_ShouldReturnCorrectUser()
        {
            var dto = new UserDto
            {
                UserName = "newuser",
                Email = "new@example.com",
                Password = "password",
                CaptchaToken = "captcha",
                Token = string.Empty
            };

            var result = await _service.BuildNewUserAsync(dto, "hashed");

            Assert.NotNull(result);
            Assert.Equal(dto.UserName, result.Username);
            Assert.Equal(dto.Email, result.Email);
            Assert.Equal("hashed", result.PasswordHash);
            Assert.Equal("User", result.Role);
            Assert.True(result.IsActive);
            Assert.Null(result.Avatar);
        }

        [Fact]
        public async Task CreateInactiveInitialSessionAsync_ShouldAttachLoginToken()
        {
            var user = CreateTestUser();

            var device = new DeviceInfo
            {
                UserAgent = "Mozilla",
                Platform = "Linux"
            };

            _tokenGeneratorServiceMock
                .Setup(x => x.GenerateJwtToken(user, device))
                .Returns("jwt_token");

            _tokenGeneratorServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            await _service.CreateInactiveInitialSessionAsync(user, device, "127.0.0.1");

            Assert.NotNull(user.LoginTokens);
            Assert.Single(user.LoginTokens);

            var token = user.LoginTokens[0];

            Assert.Equal("jwt_token", token.Token);
            Assert.Equal("refresh_token", token.RefreshToken);
            Assert.Equal(user.Id, token.UserId);
            Assert.Equal(device, token.DeviceInfo);
            Assert.Equal("127.0.0.1", token.IpAddress);
            Assert.False(token.IsActive);
        }
    }
}