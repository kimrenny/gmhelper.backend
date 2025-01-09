using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface ITokenService
    {
        public string GenerateJwtToken(User user, DeviceInfo deviceInfo);
        public string GenerateRefreshToken();
        Task<(string AccessToken, string RefreshToken)> RefreshAccessTokenAsync(string refreshToken);

    }
}