using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace MatHelper.Tests
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IAppTwoFactorSessionRepository> _twoFactorSessionRepoMock = new();
        private readonly Mock<ITwoFactorService> _twoFactorMock = new();
        private readonly Mock<IEmailLoginCodeRepository> _emailLoginCodeRepoMock = new();
        private readonly Mock<IAuthLogRepository> _authLogRepoMock = new();
        private readonly Mock<IMailService> _mailServiceMock = new();
        private readonly Mock<ISecurityService> _securityServiceMock = new();
        private readonly Mock<ILoginAttemptService> _loginAttemptServiceMock = new();
        private readonly Mock<ILogger<AuthenticationService>> _loggerMock = new();
        private readonly Mock<IRegistrationService> _registrationServiceMock = new();
        private readonly Mock<ISecurityPolicyService> _securityPolicyMock = new();
        private readonly Mock<IEmailAuthService> _emailAuthServiceMock = new();
        private readonly Mock<ILoginService> _loginServiceMock = new();
        private readonly Mock<IRecoveryService> _recoveryServiceMock = new();
        private readonly Mock<ITwoFactorAuthService> _twoFactorAuthServiceMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<ICacheService> _cacheMock = new();

        private AuthenticationService CreateService() => new(
            _userRepoMock.Object,
            _twoFactorSessionRepoMock.Object,
            _twoFactorMock.Object,
            _emailLoginCodeRepoMock.Object,
            _authLogRepoMock.Object,
            _mailServiceMock.Object,
            _securityServiceMock.Object,
            _loginAttemptServiceMock.Object,
            _registrationServiceMock.Object,
            _securityPolicyMock.Object,
            _emailAuthServiceMock.Object,
            _loginServiceMock.Object,
            _recoveryServiceMock.Object,
            _twoFactorAuthServiceMock.Object,
            _tokenServiceMock.Object,
            _cacheMock.Object,
            _loggerMock.Object
        );

        [Fact]
        public async Task RegisterUserAsync_ShouldThrow_WhenEmailIsEmpty()
        {
            var service = CreateService();

            var userDto = new UserDto
            {
                Email = "",
                UserName = "test",
                Password = "pass",
                CaptchaToken = "token",
                Token = ""
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.RegisterUserAsync(userDto, new DeviceInfo(), "127.0.0.1"));
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrow_WhenUsernameIsEmpty()
        {
            var service = CreateService();

            var userDto = new UserDto
            {
                Email = "test@test.com",
                UserName = "",
                Password = "pass",
                CaptchaToken = "token",
                Token = ""
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.RegisterUserAsync(userDto, new DeviceInfo(), "127.0.0.1"));
        }

        [Fact]
        public async Task SendRecoverPasswordLinkAsync_ShouldCallRecoveryService()
        {
            var service = CreateService();

            var email = "test@test.com";

            _recoveryServiceMock
                .Setup(x => x.SendRecoveryEmailAsync(email))
                .ReturnsAsync(true);

            var result = await service.SendRecoverPasswordLinkAsync(email);

            Assert.True(result);

            _recoveryServiceMock.Verify(
                x => x.SendRecoveryEmailAsync(email),
                Times.Once);
        }

        [Fact]
        public async Task RecoverPassword_ShouldIncrementCache_WhenSuccess()
        {
            var service = CreateService();

            _recoveryServiceMock
                .Setup(x => x.ResetPasswordAsync("token", "pass"))
                .ReturnsAsync(RecoverPasswordResult.Success);

            var result = await service.RecoverPassword("token", "pass");

            Assert.Equal(RecoverPasswordResult.Success, result);

            _cacheMock.Verify(
                x => x.IncrementVersionAsync("tokens:admin:version"),
                Times.Once);

            _cacheMock.Verify(
                x => x.IncrementVersionAsync("tokens:dashboard"),
                Times.Once);
        }
    }
}