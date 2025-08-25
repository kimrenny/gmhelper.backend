using MatHelper.CORE.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TokenValidationResult = MatHelper.CORE.Enums.TokenValidationResult;

namespace MatHelper.BLL.Interfaces
{
    public interface ITokenService
    {
        Task<(string AccessToken, string RefreshToken)> RefreshAccessTokenAsync(string refreshToken);
        Task<bool> IsTokenDisabled(string token);
        Task<TokenValidationResult> ValidateAdminAccessAsync(HttpRequest request, ClaimsPrincipal user);
        string? ExtractTokenAsync(HttpRequest request);
        Task<Guid?> GetUserIdFromTokenAsync(string authToken);
        Task<bool> HasAdminPermissionsAsync(Guid userId);
    }
}