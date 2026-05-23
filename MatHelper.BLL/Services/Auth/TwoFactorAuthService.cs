using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace MatHelper.BLL.Services
{
    public class TwoFactorAuthService : ITwoFactorAuthService
    {
        private readonly ITwoFactorService _twoFactorService;
        private readonly IUserRepository _userRepository;
        private readonly IAppTwoFactorSessionRepository _twoFactorSessionRepository;

        public TwoFactorAuthService(ITwoFactorService twoFactorService, IUserRepository userRepository, IAppTwoFactorSessionRepository twoFactorSessionRepository)
        {
            _twoFactorService = twoFactorService;
            _userRepository = userRepository;
            _twoFactorSessionRepository = twoFactorSessionRepository;
        }

        public async Task<AppTwoFactorSession> CreateTwoFactorSessionAsync(User user, DeviceInfo device, string ip, bool remember)
        {
            var sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

            var session = new AppTwoFactorSession
            {
                UserId = user.Id,
                SessionKey = sessionKey,
                Expiration = DateTime.UtcNow.AddMinutes(10),
                IpAddress = ip,
                UserAgent = device.UserAgent ?? "Unknown",
                Platform = device.Platform ?? "Unknown",
                Remember = remember,
                IsUsed = false
            };

            await _twoFactorSessionRepository.AddSessionAsync(session);

            return session;
        }

        public async Task<TwoFactorValidationResult> ValidateTwoFactorSessionAsync(string sessionKey, string code)
        {
            var session = await _twoFactorSessionRepository.GetBySessionKeyAsync(sessionKey);

            if (session == null || session.IsUsed || session.Expiration < DateTime.UtcNow)
                return new TwoFactorValidationResult
                {
                    Success = false,
                    Error = "Invalid or expired session key."
                };

            var user = await _userRepository.GetUserByIdAsync(session.UserId);
            if (user == null)
                return new TwoFactorValidationResult 
                { 
                    Success = false, 
                    Error = "User not found." 
                };

            if (user.IsBlocked)
                return new TwoFactorValidationResult 
                { 
                    Success = false, 
                    Error = "User is banned." 
                };

            var twoFactor = await _twoFactorService.GetTwoFactorAsync(user.Id, "totp");
            if (twoFactor == null || !twoFactor.IsEnabled)
                return new TwoFactorValidationResult 
                { 
                    Success = false, 
                    Error = "2FA not enabled." 
                };

            if (twoFactor.Secret == null)
                return new TwoFactorValidationResult 
                { 
                    Success = false, 
                    Error = "Invalid key." 
                };

            var isValid = _twoFactorService.VerifyTotp(twoFactor.Secret, code);
            if (!isValid)
                return new TwoFactorValidationResult 
                { 
                    Success = false, 
                    Error = "Invalid 2FA code." 
                };

            return new TwoFactorValidationResult
            {
                Success = true,
                UserId = user.Id
            };
        }
    }
}