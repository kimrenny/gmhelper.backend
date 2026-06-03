using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace MatHelper.BLL.Services
{
    public class EmailAuthService : IEmailAuthService
    {
        private readonly IMailService _mailService;
        private readonly IEmailConfirmationRepository _emailConfirmationRepository;
        private readonly IEmailLoginCodeRepository _emailLoginCodeRepository;
        private readonly ILogger _logger;

        private const byte EmailConfirmTokenLifetimeHours = 1;

        public EmailAuthService(IMailService mailService, IEmailConfirmationRepository emailConfirmationRepository, IEmailLoginCodeRepository emailLoginCodeRepository, ILogger<EmailAuthService> logger)
        {
            _mailService = mailService;
            _emailConfirmationRepository = emailConfirmationRepository;
            _emailLoginCodeRepository = emailLoginCodeRepository;
            _logger = logger;
        }

        public async Task<EmailLoginCode> CreateEmailLoginCodeAsync(User user, DeviceInfo device, string ip, bool remember)
        {
            int codeInt;
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                codeInt = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
                codeInt = 100000 + (codeInt % 900000);
            }

            var code = codeInt.ToString();
            var sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

            var emailCode = new EmailLoginCode
            {
                UserId = user.Id,
                Code = code,
                SessionKey = sessionKey,
                IpAddress = ip,
                UserAgent = device.UserAgent ?? "Unknown",
                Platform = device.Platform ?? "Unknown",
                Remember = remember,
                Expiration = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            await _emailLoginCodeRepository.AddCodeAsync(emailCode);
            await _mailService.SendIpConfirmationCodeEmailAsync(user.Email, code);

            return emailCode;
        }

        public Task<EmailConfirmationToken> CreateEmailConfirmationTokenAsync(User user)
        {
            var token = new EmailConfirmationToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = user.Id,
                ExpirationDate = DateTime.UtcNow.AddHours(EmailConfirmTokenLifetimeHours),
                IsUsed = false,
                User = user,
            };

            return Task.FromResult(token);
        }

        public async Task<ConfirmTokenResult> ConfirmEmailAsync(string token)
        {
            try
            {
                var (result, user) = await _emailConfirmationRepository.ConfirmUserByTokenAsync(token);

                if (result == ConfirmTokenResult.TokenExpired && user is not null)
                {
                    var newToken = Guid.NewGuid().ToString();

                    var newEmailToken = new EmailConfirmationToken
                    {
                        Token = newToken,
                        UserId = user.Id,
                        ExpirationDate = DateTime.UtcNow.AddHours(EmailConfirmTokenLifetimeHours),
                        IsUsed = false,
                        User = user,
                    };

                    await _emailConfirmationRepository.AddEmailConfirmationTokenAsync(newEmailToken);
                    await _emailConfirmationRepository.SaveChangesAsync();

                    await _mailService.SendConfirmationEmailAsync(user.Email, newToken);
                }

                return result;
            }
            catch (InvalidDataException)
            {
                throw new InvalidDataException("Invalid or expired token");
            }
            catch (Exception ex)
            {
                throw new Exception("Unknown error occurred during request", ex);
            }
        }
    }
}