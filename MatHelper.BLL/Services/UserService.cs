using Azure.Identity;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Repositories;
using Microsoft.IdentityModel.Tokens;
using System;
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

        public UserService(UserRepository userRepository, JwtOptions jwtOptions)
        {
            _userRepository = userRepository;
            _jwtOptions = jwtOptions;
        }

        public async Task<bool> RegisterUserAsync(UserDto userDto)
        {
            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(userDto.Email);
            if (existingUserByEmail != null) throw new InvalidOperationException("Email is already used by another user.");

            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(userDto.UserName);
            if (existingUserByUsername != null) throw new InvalidOperationException("Username is already used by another user.");

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
            return true;
        }

        public async Task<string> LoginUserAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if(!VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt)){
                throw new UnauthorizedAccessException("Invalid password.");
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
                IsActive = true
            };

            user.LoginTokens.Add(loginToken);
            await _userRepository.SaveChangesAsync();

            return accessToken;
        }

        private string GenerateJwtToken(User user, DeviceInfo deviceInfo)
        {
            var key = new SymmetricSecurityKey(Convert.FromBase64String(_jwtOptions.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
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
            return GenerateJwtToken(user, token.DeviceInfo);
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
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] saltBytes = new byte[32];
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        public async Task SaveUserAvatarAsync(string userId, byte[] avatarBytes) {
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

            return user.Avatar;
        }

        public async Task<User> GetUserDetailsAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("User not found.");

            return user;
        }
    }
}