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
        private readonly IEmailLoginCodeRepository _emailLoginCodeRepository;
        private readonly ILogger _logger;

        private const byte EmailConfirmTokenLifetimeHours = 1;

        public EmailAuthService(IMailService mailService, IEmailLoginCodeRepository emailLoginCodeRepository, ILogger<EmailAuthService> logger)
        {
            _mailService = mailService;
            _emailLoginCodeRepository = emailLoginCodeRepository;
            _logger = logger;
        }

        public async Task<EmailLoginCode> CreateEmailRegisterCodeAsync(RegisterRequestDto dto, DeviceInfo device, string ip)
        {
            await _emailLoginCodeRepository.InvalidateActiveCodesByEmailAsync(dto.Email);

            var emailCode = GenerateEmailCode(device, ip, false, null, dto.Email);

            await _emailLoginCodeRepository.AddCodeAsync(emailCode);
            await _mailService.SendRegistrationCodeEmailAsync(dto.Email, emailCode.Code);

            return emailCode;
        }

        public async Task<EmailLoginCode> CreateEmailLoginCodeAsync(User user, DeviceInfo device, string ip, bool remember)
        {
            var emailCode = GenerateEmailCode(device, ip, remember, user.Id, null);

            await _emailLoginCodeRepository.AddCodeAsync(emailCode);
            await _mailService.SendIpConfirmationCodeEmailAsync(user.Email, emailCode.Code);

            return emailCode;
        }

        private EmailLoginCode GenerateEmailCode(DeviceInfo device, string ip, bool remember, Guid? userId, string? email)
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

            return new EmailLoginCode
            {
                UserId = userId,
                Email = email,
                Code = code,
                SessionKey = sessionKey,
                IpAddress = ip,
                UserAgent = device.UserAgent ?? "Unknown",
                Platform = device.Platform ?? "Unknown",
                Remember = remember,
                Expiration = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };
        }

        public async Task<ConfirmTokenResult> ConfirmEmailAsync(string email, string code)
        {
            try
            {
                var result = await _emailLoginCodeRepository.ConfirmByEmailAndCodeAsync(email, code);

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