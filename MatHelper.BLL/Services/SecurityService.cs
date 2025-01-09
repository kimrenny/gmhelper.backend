using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MatHelper.BLL.Services
{
    public class SecurityService : ISecurityService
    {
        private readonly UserRepository _userRepository;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger _logger;

        public SecurityService(UserRepository userRepository, JwtOptions jwtOptions, ILogger<SecurityService> logger)
        {
            _userRepository = userRepository;
            _jwtOptions = jwtOptions;
            _logger = logger;
        }


        public string HashPassword(string password, string salt)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.");
            if (string.IsNullOrWhiteSpace(salt)) throw new ArgumentException("Salt is required.");

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt)))
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = hmac.ComputeHash(passwordBytes);
                var hashString = Convert.ToBase64String(hashBytes);
                return hashString;
            };
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            var hashedPassword = HashPassword(password, salt);

            return hashedPassword == hash;
        }

        public string GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] saltBytes = new byte[32];
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        public async Task<bool> CheckSuspiciousActivityAsync(string ipAddress, string userAgent, string platform)
        {
            var tokens = await _userRepository.GetAllLoginTokensAsync();
            var suspiciousAccounts = tokens.Where(t => t.IpAddress == ipAddress && t.DeviceInfo.UserAgent == userAgent && t.DeviceInfo.Platform == platform).Select(t => t.UserId).Distinct().ToList();

            if (suspiciousAccounts.Count >= 3)
            {
                foreach (var userId in suspiciousAccounts)
                {
                    var user = await _userRepository.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        user.IsBlocked = true;
                        var userTokens = user!.LoginTokens.Where(t => t.IsActive).ToList();
                        foreach (var token in userTokens)
                        {
                            token.IsActive = false;
                        }
                    }
                }

                await _userRepository.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}