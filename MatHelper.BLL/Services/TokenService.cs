using Azure.Core;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Threading.Tasks;
using TokenValidationResult = MatHelper.CORE.Enums.TokenValidationResult;

namespace MatHelper.BLL.Services
{
    public class TokenService : ITokenService
    {
        private readonly ISecurityService _securityService;
        private readonly UserRepository _userRepository;
        private readonly LoginTokenRepository _loginTokenRepository;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger _logger;

        public TokenService(ISecurityService secutiryService, UserRepository userRepository, LoginTokenRepository loginTokenRepository, JwtOptions jwtOptions, ILogger<TokenService> logger)
        {
            _securityService = secutiryService;
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _jwtOptions = jwtOptions;
            _logger = logger;
        }

        public string GenerateJwtToken(User user, DeviceInfo deviceInfo)
        {
            var key = new SymmetricSecurityKey(Convert.FromBase64String(_jwtOptions.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            if (deviceInfo.UserAgent == null || deviceInfo.Platform == null)
            {
                _logger.LogError("deviceInfo does not meet the requirements");
                throw new InvalidDataException("deviceInfo does not meet the requirements");
            }

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
            _logger.LogInformation("Attempting to refresh token: {RefreshToken}", refreshToken);

            var token = await _loginTokenRepository.GetLoginTokenByRefreshTokenAsync(refreshToken);
            if (token == null || token.RefreshTokenExpiration < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token: {RefreshToken}", refreshToken);
                throw new Exception("Invalid or expired refresh token.");
            }

            var user = token.User;
            if(user == null)
            {
                _logger.LogError("User not found for refresh token.");
                throw new Exception("User not found.");
            }

            var accessToken = GenerateJwtToken(user, token.DeviceInfo);
            var newRefreshToken = GenerateRefreshToken();

            token.Token = accessToken;
            token.Expiration = DateTime.UtcNow.AddMinutes(30);
            token.RefreshToken = newRefreshToken;

            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("New tokens generated for UserId: {UserId}", user.Id);
            return (accessToken, newRefreshToken);
        }

        public async Task<bool> IsTokenDisabled(string token)
        {
            var loginToken = await _loginTokenRepository.GetLoginTokenAsync(token);
            if (loginToken == null || loginToken.Expiration < DateTime.UtcNow || !loginToken.IsActive)
            {
                return true;
            }

            return false;
        }

        public async Task<TokenValidationResult> ValidateAdminAccessAsync(HttpRequest request, ClaimsPrincipal user)
        {
            var token = ExtractTokenAsync(request);
            if (token == null)
            {
                _logger.LogWarning("Missing Token.");
                return TokenValidationResult.MissingToken;
            }

            if (await IsTokenDisabled(token))
            {
                _logger.LogWarning("Inactive Token.");
                return TokenValidationResult.InactiveToken;
            }

            var userId = GetUserIdFromTokenAsync(user);
            if (userId == null)
            {
                _logger.LogWarning("Invalid UserId.");
                return TokenValidationResult.InvalidUserId;
            }

            if (!await HasAdminPermissionsAsync(userId.Value))
            {
                _logger.LogWarning("No Admin Permissions.");
                return TokenValidationResult.NoAdminPermissions;
            }

            return TokenValidationResult.Valid;
        }

        public string? ExtractTokenAsync(HttpRequest request)
        {
            var authorizationHeader = request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Authorization header is missing or invalid.");
                return null;
            }
            return authorizationHeader.Substring("Bearer ".Length).Trim();
        }

        public Guid? GetUserIdFromTokenAsync(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.Name);
            if(userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("User ID is not available in the token.");
                return null;
            }
            return userId;
        }

        public async Task<bool> HasAdminPermissionsAsync(Guid userId)
        {
            return await _securityService.HasAdminPermissionsAsync(userId);
        }
    }
}