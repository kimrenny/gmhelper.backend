using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(User user, DeviceInfo deviceInfo);
        string GenerateRefreshToken();
        Task<(string AccessToken, string RefreshToken)> RefreshAccessTokenAsync(string refreshToken);
        Task<bool> IsTokenDisabled(string token);

    }
}