using MatHelper.CORE.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MatHelper.BLL.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(User user, DeviceInfo deviceInfo);
        string GenerateRefreshToken();
        Task<(string AccessToken, string RefreshToken)> RefreshAccessTokenAsync(string refreshToken);
        Task<bool> IsTokenDisabled(string token);
        Task<string?> ExtractTokenAsync(HttpRequest request);
        Task<Guid?> GetUserIdFromTokenAsync(ClaimsPrincipal user);
        Task<bool> HasAdminPermissionsAsync(Guid userId);

    }
}