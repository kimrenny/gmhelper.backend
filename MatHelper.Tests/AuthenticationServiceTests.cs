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
        private readonly Mock<ITokenGeneratorService> _tokenGenMock = new();
        private readonly Mock<ITwoFactorService> _twoFactorMock = new();
        private readonly Mock<IEmailConfirmationRepository> _emailConfirmRepoMock = new();
        private readonly Mock<IEmailLoginCodeRepository> _emailLoginCodeRepoMock = new();
        private readonly Mock<IPasswordRecoveryRepository> _passwordRecoveryRepoMock = new();
        private readonly Mock<IAuthLogRepository> _authLogRepoMock = new();
        private readonly Mock<IMailService> _mailServiceMock = new();
        private readonly Mock<ISecurityService> _securityServiceMock = new();
        private readonly Mock<ILogger<AuthenticationService>> _loggerMock = new();

        private AuthenticationService CreateService() => new(
            _userRepoMock.Object,
            _twoFactorSessionRepoMock.Object,
            _tokenGenMock.Object,
            _twoFactorMock.Object,
            _emailConfirmRepoMock.Object,
            _emailLoginCodeRepoMock.Object,
            _passwordRecoveryRepoMock.Object,
            _authLogRepoMock.Object,
            _mailServiceMock.Object,
            _securityServiceMock.Object,
            _loggerMock.Object
        );

        [Fact]
        public async Task RegisterUserAsync_ShouldThrow_WhenEmailIsEmpty()
        {
            var service = CreateService();
            var userDto = new UserDto { Email = "", UserName = "test", Password = "pass", CaptchaToken = "token" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterUserAsync(userDto, new DeviceInfo(), "127.0.0.1"));
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrow_WhenUsernameIsEmpty()
        {
            var service = CreateService();
            var userDto = new UserDto { Email = "test@test.com", UserName = "", Password = "pass", CaptchaToken = "token" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterUserAsync(userDto, new DeviceInfo(), "127.0.0.1"));
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrow_WhenEmailAlreadyExists()
        {
            var service = CreateService();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Username = "existingUser",
                Role = "User",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = "hash",
                PasswordSalt = "salt",
                IsActive = true
            };
            _userRepoMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
                         .ReturnsAsync(existingUser);

            var userDto = new UserDto { Email = "test@test.com", UserName = "test", Password = "pass", CaptchaToken = "token" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterUserAsync(userDto, new DeviceInfo(), "127.0.0.1"));
        }


        [Fact]
        public async Task RegisterUserAsync_ShouldThrow_WhenUsernameAlreadyExists()
        {
            var service = CreateService();
            var userDto = new UserDto { Email = "new@test.com", UserName = "existinguser", Password = "pass", CaptchaToken = "token" };

            _userRepoMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
                         .ReturnsAsync(new User { Id = Guid.NewGuid(), Username = "existinguser", Email = "existing@test.com", PasswordHash = "hash", PasswordSalt = "salt", Role = "User", RegistrationDate = DateTime.UtcNow, LoginTokens = new List<LoginToken>() });

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterUserAsync(userDto, new DeviceInfo(), "127.0.0.1"));
        }

        [Fact]
        public async Task RegisterUserAsync_ShouldThrow_WhenTooManyRegistrationsFromIp()
        {
            var service = CreateService();
            var userDto = new UserDto { Email = "new@test.com", UserName = "newuser", Password = "pass", CaptchaToken = "token" };

            _userRepoMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepoMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepoMock.Setup(x => x.GetUserCountByIpAsync(It.IsAny<string>())).ReturnsAsync(3);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterUserAsync(userDto, new DeviceInfo(), "127.0.0.1"));
        }


        [Fact]
        public async Task RegisterUserAsync_ShouldCreateUserAndSendEmail_WhenDataIsValid()
        {
            var service = CreateService();
            var userDto = new UserDto { Email = "test@test.com", UserName = "test", Password = "pass", CaptchaToken = "token" };
            var deviceInfo = new DeviceInfo { UserAgent = "ua", Platform = "win" };

            User? addedUser = null!;
            EmailConfirmationToken? addedToken = null!;

            _userRepoMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepoMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            _userRepoMock.Setup(x => x.GetUserCountByIpAsync(It.IsAny<string>())).ReturnsAsync(0);
            _userRepoMock.Setup(x => x.AddUserAsync(It.IsAny<User>()))
                         .Callback<User>(u =>
                         {
                             u.LoginTokens = new List<LoginToken>
                             {
                                 new LoginToken
                                 {
                                     Id = 1,
                                     Token = "token",
                                     RefreshToken = "refresh",
                                     Expiration = DateTime.UtcNow.AddHours(1),
                                     RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
                                     IsActive = true,
                                     IpAddress = "127.0.0.1",
                                     UserId = u.Id,
                                     User = u,
                                     DeviceInfo = new DeviceInfo
                                     {
                                         UserAgent = "",
                                         Platform = ""
                                     }
                                 }
                             };

                             addedUser = u;
                         })
                         .Returns(Task.CompletedTask);
            _userRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _securityServiceMock.Setup(x => x.GenerateSalt()).Returns("salt");
            _securityServiceMock.Setup(x => x.HashPassword(It.IsAny<string>(), It.IsAny<string>())).Returns("hashed");

            _tokenGenMock.Setup(x => x.GenerateJwtToken(It.IsAny<User>(), It.IsAny<DeviceInfo>())).Returns("token");
            _tokenGenMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh");

            _emailConfirmRepoMock.Setup(x => x.AddEmailConfirmationTokenAsync(It.IsAny<EmailConfirmationToken>()))
                                 .Callback<EmailConfirmationToken>(t => addedToken = t)
                                 .Returns(Task.CompletedTask);
            _emailConfirmRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mailServiceMock.Setup(x => x.SendConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Task.CompletedTask);

            var result = await service.RegisterUserAsync(userDto, deviceInfo, "127.0.0.1");

            Assert.True(result);
            Assert.NotNull(addedUser);
            Assert.Equal("test", addedUser!.Username);
            Assert.Equal("test@test.com", addedUser.Email);
            Assert.Equal("hashed", addedUser.PasswordHash);
            Assert.False(addedUser.IsActive);
            Assert.NotNull(addedUser!.LoginTokens);
            Assert.Single(addedUser.LoginTokens);
            Assert.NotNull(addedToken);
            Assert.Equal(addedUser.Id, addedToken!.UserId);

            _mailServiceMock.Verify(x => x.SendConfirmationEmailAsync("test@test.com", It.IsAny<string>()), Times.Once);
            _userRepoMock.Verify(x => x.AddUserAsync(It.IsAny<User>()), Times.Once);
            _userRepoMock.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
            _emailConfirmRepoMock.Verify(x => x.AddEmailConfirmationTokenAsync(It.IsAny<EmailConfirmationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendRecoverPasswordLinkAsync_ShouldReturnFalse_WhenUserNotFound()
        {
            var service = CreateService();
            _userRepoMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);

            var result = await service.SendRecoverPasswordLinkAsync("nonexistent@test.com");

            Assert.False(result);
        }

        [Fact]
        public async Task SendRecoverPasswordLinkAsync_ShouldSendEmail_WhenUserExists()
        {
            var service = CreateService();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "User",
                RegistrationDate = DateTime.UtcNow,
                IsBlocked = false,
                LoginTokens = new List<LoginToken>(),
                Avatar = null
            };

            _userRepoMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _passwordRecoveryRepoMock.Setup(x => x.AddPasswordRecoveryTokenAsync(It.IsAny<PasswordRecoveryToken>())).Returns(Task.CompletedTask);
            _passwordRecoveryRepoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mailServiceMock.Setup(x => x.SendPasswordRecoveryEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var result = await service.SendRecoverPasswordLinkAsync(user.Email);

            Assert.True(result);
            _mailServiceMock.Verify(x => x.SendPasswordRecoveryEmailAsync(user.Email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RecoverPassword_ShouldReturnFailed_WhenExceptionThrown()
        {
            var service = CreateService();
            _passwordRecoveryRepoMock.Setup(x => x.GetUserByRecoveryToken(It.IsAny<string>())).ThrowsAsync(new Exception());

            var result = await service.RecoverPassword("token", "newpass");

            Assert.Equal(RecoverPasswordResult.Failed, result);
        }

        [Fact]
        public async Task RecoverPassword_ShouldChangePassword_WhenTokenValid()
        {
            var service = CreateService();
            var user = new User {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                Role = "User",
                RegistrationDate = DateTime.UtcNow,
                IsBlocked = false,
                LoginTokens = new List<LoginToken>(),
                Avatar = null
            };
            _passwordRecoveryRepoMock.Setup(x => x.GetUserByRecoveryToken(It.IsAny<string>()))
                .ReturnsAsync((RecoverPasswordResult.Success, user));

            _userRepoMock.Setup(x => x.ChangePassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _securityServiceMock.Setup(x => x.GenerateSalt()).Returns("salt");
            _securityServiceMock.Setup(x => x.HashPassword(It.IsAny<string>(), "salt")).Returns("hashed");

            var result = await service.RecoverPassword("token", "newpass");

            Assert.Equal(RecoverPasswordResult.Success, result);
            _userRepoMock.Verify(x => x.ChangePassword(user, "hashed", "salt"), Times.Once);
        }
    }
}
