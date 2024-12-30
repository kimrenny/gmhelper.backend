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
    public class UserService : IUserService
    {
        private readonly UserRepository _userRepository;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger _logger;

        public UserService(UserRepository userRepository, JwtOptions jwtOptions, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _jwtOptions = jwtOptions;
            _logger = logger;
        }

        public async Task<bool> RegisterUserAsync(UserDto userDto)
        {
            if (string.IsNullOrWhiteSpace(userDto.Email))
                throw new ArgumentException("Email cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(userDto.UserName))
                throw new ArgumentException("Username cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(userDto.IpAddress))
                throw new ArgumentException("IP Address cannot be null or empty.");

            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(userDto.Email);
            if (existingUserByEmail != null) throw new InvalidOperationException("Email is already used by another user.");

            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(userDto.UserName);
            if (existingUserByUsername != null) throw new InvalidOperationException("Username is already used by another user.");

            var userCountByIp = await _userRepository.GetUserCountByIpAsync(userDto.IpAddress);

            if (userCountByIp >= 3)
            {
                var usersToBlock = await _userRepository.GetUsersByIpAsync(userDto.IpAddress);

                if (usersToBlock == null || !usersToBlock.Any())
                    throw new InvalidOperationException("No users found with the specified IP address.");

                if(usersToBlock != null && usersToBlock.Count > 0)
                {
                    foreach (var blockedUser in usersToBlock)
                    {
                        if (blockedUser != null)
                        {
                            blockedUser.IsBlocked = true;
                            IEnumerable<LoginToken> tokens = blockedUser.LoginTokens.Where(t => t.IsActive);
                            if (tokens.Count() > 0)
                            {
                                foreach (var token in tokens)
                                {
                                    if (token != null)
                                        token.IsActive = false;
                                }
                            }
                        }
                    }
                }

                await _userRepository.SaveChangesAsync();
                throw new UnauthorizedAccessException("Violation of service rules. All user accounts have been blocked.");
            }

            var salt = GenerateSalt();
            var hashedPassword = HashPassword(userDto.Password, salt);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = userDto.UserName,
                Email = userDto.Email,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                Avatar = null,
                Role = "User",
            };

            await _userRepository.AddUserAsync(user);

            var loginToken = new LoginToken
            {
                Token = GenerateJwtToken(user, userDto.DeviceInfo),
                RefreshToken = GenerateRefreshToken(),
                Expiration = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                DeviceInfo = userDto.DeviceInfo,
                IpAddress = userDto.IpAddress,
                IsActive = true
            };

            var createdUser = await _userRepository.GetUserByEmailAsync(userDto.Email);

            if (createdUser != null)
            {
                createdUser!.LoginTokens.Add(loginToken);
                await _userRepository.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException("Error due add token to the user.");
            }

            if (await CheckSuspiciousActivityAsync(userDto.IpAddress, userDto.DeviceInfo.UserAgent, userDto.DeviceInfo.Platform))
            {
                throw new UnauthorizedAccessException("Suspicious activity detected. Accounts blocked.");
            }

            return true;
        }

        public async Task<string> LoginUserAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (user.IsBlocked)
            {
                throw new UnauthorizedAccessException("User is blocked");
            }

            if (!VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
            {
                throw new UnauthorizedAccessException("Invalid password.");
            }

            var existingToken = user.LoginTokens.Where(t => t.DeviceInfo.UserAgent == loginDto.DeviceInfo.UserAgent && t.DeviceInfo.Platform == loginDto.DeviceInfo.Platform && t.IsActive && t.Expiration > DateTime.UtcNow && t.IpAddress == loginDto.IpAddress).OrderByDescending(t => t.Expiration).FirstOrDefault();

            if (existingToken != null)
            {
                if (existingToken.Expiration > DateTime.UtcNow)
                {
                    return existingToken.Token;
                }

                existingToken.Token = GenerateJwtToken(user, loginDto.DeviceInfo);
                existingToken.Expiration = DateTime.UtcNow.AddMinutes(15);
                existingToken.RefreshToken = GenerateRefreshToken();
                existingToken.RefreshTokenExpiration = DateTime.UtcNow.AddDays(7);
                existingToken.IpAddress = loginDto.IpAddress;
                await _userRepository.SaveChangesAsync();

                return existingToken.Token;
            }

            var accessToken = GenerateJwtToken(user, loginDto.DeviceInfo);
            var refreshToken = GenerateRefreshToken();

            var loginToken = new LoginToken
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                DeviceInfo = loginDto.DeviceInfo,
                IpAddress = loginDto.IpAddress,
                IsActive = true
            };

            user.LoginTokens.Add(loginToken);
            await _userRepository.SaveChangesAsync();

            if (await CheckSuspiciousActivityAsync(loginDto.IpAddress, loginDto.DeviceInfo.UserAgent, loginDto.DeviceInfo.Platform))
            {
                throw new UnauthorizedAccessException("Suspicious activity detected. Accounts blocked.");
            }

            return accessToken;
        }

        private string GenerateJwtToken(User user, DeviceInfo deviceInfo)
        {
            var key = new SymmetricSecurityKey(Convert.FromBase64String(_jwtOptions.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user!.Role),
                new Claim("Device", deviceInfo.UserAgent),
                new Claim("Platform", deviceInfo.Platform)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        public async Task<bool> RecoverPasswordAsync(PasswordRecoveryDto recoveryDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(recoveryDto.Email);
            if (user == null) return false;

            return true;
        }

        public async Task<string> RefreshAccessTokenAsync(string refreshToken)
        {
            var token = await _userRepository.GetLoginTokenByRefreshTokenAsync(refreshToken);
            if (token == null || token.RefreshTokenExpiration < DateTime.UtcNow)
                throw new Exception("Invalid or expired refresh token.");

            var user = await _userRepository.GetUserByIdAsync(token.UserId);
            if(user != null)
            {
                return GenerateJwtToken(user, token.DeviceInfo);
            }
            else
            {
                throw new Exception("User not found.");
            }
        }

        private string HashPassword(string password, string salt)
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

        private bool VerifyPassword(string password, string hash, string salt)
        {
            var hashedPassword = HashPassword(password, salt);

            return hashedPassword == hash;
        }

        private string GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] saltBytes = new byte[32];
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        private async Task<bool> CheckSuspiciousActivityAsync(string ipAddress, string userAgent, string platform)
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

        public async Task SaveUserAvatarAsync(string userId, byte[] avatarBytes)
        {
            var user = await _userRepository.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null) throw new Exception("User not found.");

            user.Avatar = avatarBytes;
            await _userRepository.SaveChangesAsync();
        }

        public async Task<byte[]> GetUserAvatarAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            return user!.Avatar;
        }

        public async Task<User> GetUserDetailsAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            return user;
        }
    }
}