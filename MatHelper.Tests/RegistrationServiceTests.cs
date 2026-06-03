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
        private readonly Mock<IEmailConfirmationRepository> _emailConfirmationRepositoryMock = new();

        private readonly RegistrationService _service;

        public RegistrationServiceTests()
        {
            _service = new RegistrationService(
                _tokenGeneratorServiceMock.Object,
                _userRepositoryMock.Object,
                _emailConfirmationRepositoryMock.Object
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
            IsActive = isActive
        };

        [Fact]
        public async Task EnsureEmailAndUsernameUniqueAsync_ShouldNotThrow_WhenEmailAndUsernameAreUnique()
        {
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User)null!);

            var exception = await Record.ExceptionAsync(() => 
                _service.EnsureEmailAndUsernameUniqueAsync("new@example.com", "newuser"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task EnsureEmailAndUsernameUniqueAsync_ShouldThrowInvalidOperationException_WhenEmailIsUsedByActiveUser()
        {
            var email = "active@example.com";
            var activeUser = CreateTestUser(email, "activeuser", isActive: true);

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(activeUser);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.EnsureEmailAndUsernameUniqueAsync(email, "anyuser"));

            Assert.Equal("Email is already used by another user.", exception.Message);
        }

        [Fact]
        public async Task EnsureEmailAndUsernameUniqueAsync_ShouldThrowInvalidOperationException_WhenAccountAwaitsConfirmation()
        {
            var email = "pending@example.com";
            var inactiveUser = CreateTestUser(email, "pendinguser", isActive: false);
            var validToken = new EmailConfirmationToken
            {
                Token = "token",
                UserId = inactiveUser.Id,
                ExpirationDate = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                User = inactiveUser
            };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(inactiveUser);
            _emailConfirmationRepositoryMock.Setup(x => x.GetTokenByUserIdAsync(inactiveUser.Id)).ReturnsAsync(validToken);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.EnsureEmailAndUsernameUniqueAsync(email, "anyuser"));

            Assert.Equal("The account awaits confirmation. Follow the link in the email.", exception.Message);
        }

        [Fact]
        public async Task EnsureEmailAndUsernameUniqueAsync_ShouldDeleteAndDetachUser_WhenTokenIsExpiredOrNull()
        {
            var email = "expired@example.com";
            var username = "newuser";
            var inactiveUser = CreateTestUser(email, "expireduser", isActive: false);
            var expiredToken = new EmailConfirmationToken
            {
                Token = "token",
                UserId = inactiveUser.Id,
                ExpirationDate = DateTime.UtcNow.AddMinutes(-5),
                IsUsed = false,
                User = inactiveUser
            };

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(inactiveUser);
            _emailConfirmationRepositoryMock.Setup(x => x.GetTokenByUserIdAsync(inactiveUser.Id)).ReturnsAsync(expiredToken);
            _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(username)).ReturnsAsync((User)null!);

            await _service.EnsureEmailAndUsernameUniqueAsync(email, username);

            _userRepositoryMock.Verify(x => x.DeleteUserAsync(inactiveUser), Times.Once);
            _userRepositoryMock.Verify(x => x.Detach(inactiveUser), Times.Once);
        }

        [Fact]
        public async Task EnsureEmailAndUsernameUniqueAsync_ShouldThrowInvalidOperationException_WhenUsernameIsAlreadyUsed()
        {
            var username = "existinguser";
            var existingUser = CreateTestUser("other@example.com", username);

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepositoryMock.Setup(x => x.GetUserByUsernameAsync(username)).ReturnsAsync(existingUser);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.EnsureEmailAndUsernameUniqueAsync("new@example.com", username));

            Assert.Equal("Username is already used by another user.", exception.Message);
        }

        [Fact]
        public async Task BuildNewUserAsync_ShouldReturnCorrectlyMappedUser()
        {
            var dto = new UserDto { UserName = "newuser", Email = "new@example.com", Password = "password", CaptchaToken = "captcha" };
            var passwordHash = "hashed_password";

            var result = await _service.BuildNewUserAsync(dto, passwordHash);

            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(dto.UserName, result.Username);
            Assert.Equal(dto.Email, result.Email);
            Assert.Equal(passwordHash, result.PasswordHash);
            Assert.Equal("User", result.Role);
            Assert.False(result.IsActive);
            Assert.Null(result.Avatar);
            Assert.True(result.RegistrationDate > DateTime.UtcNow.AddSeconds(-5));
        }

        [Fact]
        public async Task CreateInactiveInitialSessionAsync_ShouldAddInactiveTokenToUser()
        {
            var user = CreateTestUser();
            var device = new DeviceInfo { UserAgent = "Mozilla", Platform = "Linux" };
            var ip = "127.0.0.1";

            _tokenGeneratorServiceMock.Setup(x => x.GenerateJwtToken(user, device)).Returns("jwt_token");
            _tokenGeneratorServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            await _service.CreateInactiveInitialSessionAsync(user, device, ip);

            Assert.NotNull(user.LoginTokens);
            var token = Assert.Single(user.LoginTokens);
            Assert.Equal("jwt_token", token.Token);
            Assert.Equal("refresh_token", token.RefreshToken);
            Assert.Equal(user.Id, token.UserId);
            Assert.Equal(device, token.DeviceInfo);
            Assert.Equal(ip, token.IpAddress);
            Assert.False(token.IsActive);
            Assert.True(token.Expiration > DateTime.UtcNow.AddSeconds(-5));
            Assert.True(token.RefreshTokenExpiration > DateTime.UtcNow.AddSeconds(-5));
        }
    }
}