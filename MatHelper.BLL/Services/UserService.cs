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
            var existingUser = await _userRepository.GetUserByEmailAsync(userDto.Email);
            if (existingUser != null) return false;

            var salt = GenerateSalt();
            var hashedPassword = HashPassword(userDto.Password, salt);
            var user = new User
            {
                Username = userDto.UserName,
                Email = userDto.Email,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                Avatar = [],
                Role = "User",
            };

            await _userRepository.AddUserAsync(user);
            return true;
        }

        public async Task<string> LoginUserAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);
            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt)) throw new Exception("Invalid credentials.");

            var accessToken = GenerateJwtToken(user, loginDto.DeviceInfo);
            var refreshToken = GenerateRefreshToken();

            var loginToken = new LoginToken
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(15),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                DeviceInfo = loginDto.DeviceInfo
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
            using var sha256 = SHA256.Create();
            var combined = password + salt;
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(combined)));
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            return HashPassword(password, salt) == hash;
        }

        private string GenerateSalt()
        {
            return Guid.NewGuid().ToString("N");
        }

        public async Task SaveUserAvatarAsync(string userId, byte[] avatarBytes) {
            var user = await _userRepository.GetUserByIdAsync(int.Parse(userId));
            if (user == null) throw new Exception("User not found.");

            user.Avatar = avatarBytes;
            await _userRepository.SaveChangesAsync();
        }

        public async Task<byte[]> GetUserAvatarAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(int.Parse(userId));
            if (user == null)
                throw new Exception("User not found.");

            return user.Avatar;
        }

        public async Task<User> GetUserDetailsAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(int.Parse(userId));
            if (user == null) throw new Exception("User not found.");

            return user;
        }
    }
}