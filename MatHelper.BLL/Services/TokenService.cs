using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TokenValidationResult = MatHelper.CORE.Enums.TokenValidationResult;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;

namespace MatHelper.BLL.Services
{
    public class TokenService : ITokenService
    {
        private readonly ISecurityService _securityService;
        private readonly ITokenGeneratorService _tokenGeneratorService;
        private readonly IUserRepository _userRepository;
        private readonly ILoginTokenRepository _loginTokenRepository;
        private readonly ILogger _logger;

        public TokenService(ISecurityService secutiryService, ITokenGeneratorService tokenGeneratorService, IUserRepository userRepository, ILoginTokenRepository loginTokenRepository, ILogger<TokenService> logger)
        {
            _securityService = secutiryService;
            _tokenGeneratorService = tokenGeneratorService;
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _logger = logger;
        }

        public async Task<LoginResponse> RefreshAccessTokenAsync(string refreshToken)
        {
            //_logger.LogInformation("Attempting to refresh token: {RefreshToken}", refreshToken);

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

            var accessToken = _tokenGeneratorService.GenerateJwtToken(user, token.DeviceInfo);
            var newRefreshToken = _tokenGeneratorService.GenerateRefreshToken();

            token.Token = accessToken;
            token.Expiration = DateTime.UtcNow.AddMinutes(30);
            token.RefreshToken = newRefreshToken;

            await _userRepository.SaveChangesAsync();

            //_logger.LogInformation("New tokens generated for UserId: {UserId}", user.Id);
            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiration = token.RefreshTokenExpiration
            };
        }

        public async Task<bool> DisableRefreshToken(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return false;

            var token = await _loginTokenRepository.GetLoginTokenByRefreshTokenAsync(refreshToken);
            if (token == null || token.RefreshTokenExpiration < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token: {RefreshToken}", refreshToken);
                throw new Exception("Invalid or expired refresh token.");
            }

            await _loginTokenRepository.DeactivateTokenAsync(token);
            return true;
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

            var userId = await GetUserIdFromTokenAsync(token);
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

        public async Task<Guid?> GetUserIdFromTokenAsync(string authToken)
        {
            return await _loginTokenRepository.GetUserIdByAuthTokenAsync(authToken);
        }

        public async Task<bool> HasAdminPermissionsAsync(Guid userId)
        {
            return await _securityService.HasAdminPermissionsAsync(userId);
        }
    }
}