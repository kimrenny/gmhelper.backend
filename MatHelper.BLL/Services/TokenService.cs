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
    public class TokenService : ITokenService
    {
        private readonly UserRepository _userRepository;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger _logger;

        public TokenService(UserRepository userRepository, JwtOptions jwtOptions, ILogger<TokenService> logger)
        {
            _userRepository = userRepository;
            _jwtOptions = jwtOptions;
            _logger = logger;
        }

        public string GenerateJwtToken(User user, DeviceInfo deviceInfo)
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
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        public async Task<(string AccessToken, string RefreshToken)> RefreshAccessTokenAsync(string refreshToken)
        {
            _logger.LogInformation("Searching for refresh token: {RefreshToken}", refreshToken);

            var token = await _userRepository.GetLoginTokenByRefreshTokenAsync(refreshToken);
            if (token == null || token.RefreshTokenExpiration < DateTime.UtcNow)
            {
                if (token != null)
                {
                    _logger.LogWarning("Refresh token expired: {RefreshToken}", refreshToken);
                    _userRepository.RemoveToken(token);
                    await _userRepository.SaveChangesAsync();
                    throw new Exception("Invalid or expired refresh token.");
                }
                _logger.LogWarning("Refresh token not found or expired: {RefreshToken}", refreshToken);
                throw new Exception("Invalid or expired refresh token.");
            }

            _logger.LogInformation("Refresh token valid. Fetching user: {UserId}", token.UserId);
            var user = await _userRepository.GetUserByIdAsync(token.UserId);

            if (user != null)
            {
                _logger.LogInformation("User found. Generating new tokens for UserId: {UserId}", token.UserId);

                var accessToken = GenerateJwtToken(user, token.DeviceInfo);
                var newRefreshToken = GenerateRefreshToken();
                token.Token = accessToken;
                token.Expiration = DateTime.UtcNow.AddMinutes(30);
                token.RefreshToken = newRefreshToken;
                token.RefreshTokenExpiration = DateTime.UtcNow.AddDays(7);

                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("New tokens generated for UserId: {UserId}", token.UserId);

                return (accessToken, newRefreshToken);
            }
            else
            {
                _logger.LogError("User not found for token refresh. UserId: {UserId}", token.UserId);
                throw new Exception("User not found.");
            }
        }

        public async Task<bool> IsTokenDisabled(string token)
        {
            var loginToken = await _userRepository.GetLoginTokenAsync(token);
            if (loginToken == null || loginToken.Expiration < DateTime.UtcNow || !loginToken.IsActive)
            {
                return true;
            }

            return false;
        }
    }
}