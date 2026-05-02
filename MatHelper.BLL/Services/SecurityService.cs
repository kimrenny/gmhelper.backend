using MatHelper.BLL.Interfaces;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using System.Text;

namespace MatHelper.BLL.Services
{
    public class SecurityService : ISecurityService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginTokenRepository _loginTokenRepository;
        private readonly ILogger _logger;

        private const ushort SaltSizeInBytes = 16;
        private const ushort MaxUsersPerDevice = 3;

        private const string GeoApiUrl = "https://get.geojs.io/v1/ip/country.json?ip=";

        public SecurityService(IUserRepository userRepository, ILoginTokenRepository loginTokenRepository, ILogger<SecurityService> logger)
        {
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _logger = logger;
        }


        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.");

            var salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 8,
                Iterations = 4,
                MemorySize = 1024 * 64
            };

            var hash = argon2.GetBytes(32);

            var result = Convert.ToBase64String(salt.Concat(hash).ToArray());
            return result;
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            var fullBytes = Convert.FromBase64String(storedHash);

            var salt = fullBytes.Take(16).ToArray();
            var hash = fullBytes.Skip(16).ToArray();

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 8,
                Iterations = 4,
                MemorySize = 1024 * 64
            };

            var computedHash = argon2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }

        public async Task<bool> CheckSuspiciousActivityAsync(string ipAddress, string userAgent, string platform)
        {
            var tokens = await _loginTokenRepository.GetAllLoginTokensAsync();
            var suspiciousAccounts = tokens
                .Where(t => t.IpAddress == ipAddress && t.DeviceInfo.UserAgent == userAgent && t.DeviceInfo.Platform == platform)
                .Select(t => t.UserId)
                .Distinct()
                .ToList();

            if (suspiciousAccounts.Count > MaxUsersPerDevice)
            {
                foreach (var userId in suspiciousAccounts)
                {
                    var user = await _userRepository.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        user.IsBlocked = true;
                        if (user.LoginTokens != null)
                        {
                            var userTokens = user.LoginTokens.Where(t => t.IsActive).ToList();
                            foreach (var token in userTokens)
                            {
                                token.IsActive = false;
                            }
                        }
                    }
                }

                await _userRepository.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> HasAdminPermissionsAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null)
            {
                return false;
            }

            return user.Role == "Admin" || user.Role == "Owner";
        }

        public async Task<string> GetCountryByIpAsync(string ipAddress)
        {
            try
            {
                if(ipAddress == "127.0.0.1")
                {
                    return "localhost";
                }

                using var client = new HttpClient();
                var url = $"{GeoApiUrl}{ipAddress}";
                var response = await client.GetStringAsync(url);

                var jsonArray = JArray.Parse(response);
                var country = jsonArray.FirstOrDefault()?["name"]?.ToString();

                return string.IsNullOrWhiteSpace(country) ? "Unknown" : country;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing response for IP: {ipAddress}");
                return "Unknown";
            }
        }
    }
}