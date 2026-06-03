using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MatHelper.Tests
{
    public class RecoveryServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPasswordRecoveryRepository> _passwordRecoveryRepositoryMock = new();
        private readonly Mock<IMailService> _mailServiceMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ISecurityService> _securityServiceMock = new();
        private readonly Mock<ILogger<RecoveryService>> _loggerMock = new();

        private readonly RecoveryService _service;

        public RecoveryServiceTests()
        {
            _service = new RecoveryService(
                _userRepositoryMock.Object,
                _passwordRecoveryRepositoryMock.Object,
                _mailServiceMock.Object,
                _tokenServiceMock.Object,
                _securityServiceMock.Object,
                _loggerMock.Object
            );
        }

        private User CreateTestUser() => new()
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "testuser",
            PasswordHash = "old_hash",
            Role = "User",
            RegistrationDate = DateTime.UtcNow
        };

        [Fact]
        public async Task CreateRecoveryTokenAsync_ShouldReturnValidTokenAndSaveToDb()
        {
            var user = CreateTestUser();

            var result = await _service.CreateRecoveryTokenAsync(user);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(user, result.User);
            Assert.False(result.IsUsed);
            Assert.True(Guid.TryParse(result.Token, out _));
            Assert.True(result.ExpirationDate > DateTime.UtcNow.AddMinutes(14));

            _passwordRecoveryRepositoryMock.Verify(x => x.AddPasswordRecoveryTokenAsync(result), Times.Once);
            _passwordRecoveryRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendRecoveryEmailAsync_ShouldReturnTrueAndSendEmail_WhenUserExists()
        {
            var email = "user@example.com";
            var user = CreateTestUser();

            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync(user);

            var result = await _service.SendRecoveryEmailAsync(email);

            Assert.True(result);
            _passwordRecoveryRepositoryMock.Verify(x => x.AddPasswordRecoveryTokenAsync(It.IsAny<PasswordRecoveryToken>()), Times.Once);
            _passwordRecoveryRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
            _mailServiceMock.Verify(x => x.SendPasswordRecoveryEmailAsync(user.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendRecoveryEmailAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            var email = "nonexistent@example.com";
            _userRepositoryMock.Setup(x => x.GetUserByEmailAsync(email)).ReturnsAsync((User)null!);

            var result = await _service.SendRecoveryEmailAsync(email);

            Assert.False(result);
            _passwordRecoveryRepositoryMock.Verify(x => x.AddPasswordRecoveryTokenAsync(It.IsAny<PasswordRecoveryToken>()), Times.Never);
            _mailServiceMock.Verify(x => x.SendPasswordRecoveryEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task SendRecoveryEmailAsync_ShouldThrowArgumentException_WhenEmailIsEmpty(string? invalidEmail)
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.SendRecoveryEmailAsync(invalidEmail!));
            Assert.Equal("Email cannot be null or empty.", exception.Message);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldChangePasswordAndDeactivateTokens_WhenTokenIsValid()
        {
            var token = "valid_token";
            var newPassword = "NewPassword123!";
            var user = CreateTestUser();
            var hashedPassword = "new_hashed_password";

            _passwordRecoveryRepositoryMock
                .Setup(x => x.GetUserByRecoveryToken(token))
                .ReturnsAsync((RecoverPasswordResult.Success, user));

            _securityServiceMock.Setup(x => x.HashPassword(newPassword)).Returns(hashedPassword);

            var result = await _service.ResetPasswordAsync(token, newPassword);

            Assert.Equal(RecoverPasswordResult.Success, result);
            _userRepositoryMock.Verify(x => x.ChangePassword(user, hashedPassword), Times.Once);
            _passwordRecoveryRepositoryMock.Verify(x => x.InvalidateAllUserRecoveryTokensAsync(user.Id), Times.Once);
            _tokenServiceMock.Verify(x => x.DeactivateAllUserTokensAsync(user.Id), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnResult_WhenUserNotFoundByToken()
        {
            var token = "invalid_token";
            _passwordRecoveryRepositoryMock
                .Setup(x => x.GetUserByRecoveryToken(token))
                .ReturnsAsync((RecoverPasswordResult.Failed, (User)null!));

            var result = await _service.ResetPasswordAsync(token, "any_pass");

            Assert.Equal(RecoverPasswordResult.Failed, result);
            _userRepositoryMock.Verify(x => x.ChangePassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            _passwordRecoveryRepositoryMock.Verify(x => x.InvalidateAllUserRecoveryTokensAsync(It.IsAny<Guid>()), Times.Never);
            _tokenServiceMock.Verify(x => x.DeactivateAllUserTokensAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnExpired_WhenTokenIsExpiredButUserFound()
        {
            var token = "expired_token";
            var user = CreateTestUser();

            _passwordRecoveryRepositoryMock
                .Setup(x => x.GetUserByRecoveryToken(token))
                .ReturnsAsync((RecoverPasswordResult.TokenExpired, user));

            var result = await _service.ResetPasswordAsync(token, "any_pass");

            Assert.Equal(RecoverPasswordResult.TokenExpired, result);
            _userRepositoryMock.Verify(x => x.ChangePassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            _passwordRecoveryRepositoryMock.Verify(x => x.InvalidateAllUserRecoveryTokensAsync(It.IsAny<Guid>()), Times.Never);
            _tokenServiceMock.Verify(x => x.DeactivateAllUserTokensAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}