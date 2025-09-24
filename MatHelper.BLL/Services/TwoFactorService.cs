using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;
using OtpNet;
using MatHelper.DAL.Interfaces;
using MatHelper.BLL.Interfaces;

namespace MatHelper.BLL.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly ITwoFactorRepository _twoFactorRepository;
        private readonly ILogger _logger;

        public TwoFactorService(ITwoFactorRepository twoFactorRepository, ILogger<ITwoFactorService> logger)
        {
            _twoFactorRepository = twoFactorRepository;
            _logger = logger;
        }

        public async Task<UserTwoFactor> GenerateTwoFAKeyAsync(Guid userId, string type)
        {
            var existingKey = await _twoFactorRepository.GetTwoFactorAsync(userId, type);

            if(existingKey != null)
            {
                if (existingKey.IsEnabled)
                {
                    throw new InvalidOperationException("Two-factor authentication is already enabled.");
                }

                if((DateTime.UtcNow - existingKey.CreatedAt).TotalMinutes > 10)
                {
                    _twoFactorRepository.Remove(existingKey);
                    await _twoFactorRepository.SaveChangesAsync();
                    existingKey = null;
                }
            }

            if(existingKey == null) 
            {
                var secret = GenerateSecret();
                var twoFactor = new UserTwoFactor
                {
                    UserId = userId,
                    Type = type,
                    Secret = secret,
                    IsEnabled = false,
                    AlwaysAsk = true,
                    CreatedAt = DateTime.UtcNow,
                };

                await _twoFactorRepository.AddTwoFactorAsync(twoFactor);
                return twoFactor;
            }

            return existingKey;
        }

        public async Task<bool> VerifyTwoFACodeAsync(Guid userId, string type, string code)
        {
            var twoFactor = await _twoFactorRepository.GetTwoFactorAsync(userId, type);

            if (twoFactor == null) throw new InvalidOperationException("Two-factor authentication not found for this user.");

            if (twoFactor.IsEnabled) throw new InvalidOperationException("Two-factor authentication is already enabled.");

            bool isValid = VerifyTotp(twoFactor.Secret!, code);
            if (isValid)
            {
                twoFactor.IsEnabled = true;
                twoFactor.UpdatedAt = DateTime.UtcNow;
                await _twoFactorRepository.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException("Invalid 2FA code.");
            }

            return isValid;
        }

        public async Task ChangeTwoFactorModeAsync(Guid userId, string type, bool alwaysAsk)
        {
            await _twoFactorRepository.UpdateTwoFactorModeAsync(userId, type, alwaysAsk);
        }

        public string GenerateQrCode(string secret, string userEmail)
        {
            string issuer = "MatHelper";
            string totpUri = $"otpauth://totp/{issuer}:{userEmail}?secret={secret}&issuer={issuer}";

            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.Base64QRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        public async Task<UserTwoFactor?> GetTwoFactorAsync(Guid userId, string type)
        {
            return await _twoFactorRepository.GetTwoFactorAsync(userId, type);
        }

        public async Task RemoveTwoFactorAsync(UserTwoFactor twoFactor)
        {
            _twoFactorRepository.Remove(twoFactor);
            await _twoFactorRepository.SaveChangesAsync();
        }

        public bool VerifyTotp(string secret, string code)
        {
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }

        private string GenerateSecret()
        {
            var bytes = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(bytes);
        }
    }
}