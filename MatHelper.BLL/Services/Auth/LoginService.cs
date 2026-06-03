using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace MatHelper.BLL.Services
{
    public class LoginService : ILoginService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenGeneratorService _tokenGenerator;
        private readonly ISecurityService _securityService;
        private readonly ILogger _logger;

        private const byte MaxTotalActiveTokens = 5;
        private const ushort RefreshTokenLifetimeDaysRemembered = 28;
        private const ushort RefreshTokenLifetimeHoursDefault = 6;
        private const ushort AccessTokenLifetimeMinutes = 30;

        public LoginService(IUserRepository userRepository, ITokenGeneratorService tokenGenerator, ISecurityService securityService, ILogger<LoginService> logger)
        {
            _userRepository = userRepository;
            _tokenGenerator = tokenGenerator;
            _securityService = securityService;
            _logger = logger;
        }

        public async Task<User> ValidateUserCredentialsAsync(string email, string password)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (!_securityService.VerifyPassword(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid password.");

            return user;
        }

        public Task EnsureUserCanLoginAsync(User user)
        {
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Please activate your account by following the link sent to your email.");

            if (user.IsBlocked)
                throw new UnauthorizedAccessException("User is banned.");

            return Task.CompletedTask;
        }

        public Task<LoginToken> IssueLoginTokenAsync(User user, DeviceInfo device, string ip, bool remember)
        {
            var accessToken = _tokenGenerator.GenerateJwtToken(user, device);
            var refreshToken = _tokenGenerator.GenerateRefreshToken();

            var refreshExp = remember
                ? DateTime.UtcNow.AddDays(RefreshTokenLifetimeDaysRemembered)
                : DateTime.UtcNow.AddHours(RefreshTokenLifetimeHoursDefault);

            var token = new LoginToken
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(AccessTokenLifetimeMinutes),
                RefreshTokenExpiration = refreshExp,
                UserId = user.Id,
                DeviceInfo = device,
                IpAddress = ip,
                IsActive = true
            };

            return Task.FromResult(token);
        }

        public async Task CleanupUserSessionsAsync(User user, DeviceInfo device, string ip)
        {
            var expiredTokens = user.LoginTokens!.Where(t => t.Expiration <= DateTime.UtcNow).ToList();
            foreach (var token in expiredTokens)
                token.IsActive = false;

            var duplicateGroups = user.LoginTokens!
                .Where(t => t.IsActive)
                .GroupBy(t => new { t.DeviceInfo.UserAgent, t.DeviceInfo.Platform, t.IpAddress });

            foreach (var group in duplicateGroups)
            {
                var latest = group.OrderByDescending(t => t.Expiration).FirstOrDefault();
                foreach (var token in group)
                {
                    if (token != latest)
                        token.IsActive = false;
                }
            }

            var currentDeviceTokens = user.LoginTokens!
                .Where(t =>
                    t.IsActive &&
                    t.DeviceInfo.UserAgent == device.UserAgent &&
                    t.DeviceInfo.Platform == device.Platform &&
                    t.IpAddress == ip)
                .ToList();

            foreach (var token in currentDeviceTokens)
                token.IsActive = false;

            await _userRepository.SaveChangesAsync();
        }

        public Task ApplySessionLimitsAsync(User user)
        {
            var activeTokens = user.LoginTokens!.Where(t => t.IsActive).ToList();

            if (activeTokens.Count >= 5)
            {
                var oldest = activeTokens.MinBy(t => t.Expiration);
                if (oldest != null)
                    oldest.IsActive = false;
            }

            return Task.CompletedTask;
        }
    }
}