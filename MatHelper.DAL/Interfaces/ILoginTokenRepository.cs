using MatHelper.CORE.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface ILoginTokenRepository
    {
        Task<LoginToken?> GetLoginTokenByRefreshTokenAsync(string refreshToken);
        Task<LoginToken?> GetLoginTokenAsync(string token);
        Task RemoveLoginTokenAsync(LoginToken token);
        Task<List<LoginToken>> GetAllLoginTokensAsync();
        Task ActionTokenAsync(string authToken, string action);
        Task<DashboardTokensDto> GetDashboardTokensAsync();
        Task<List<UserIp>> GetUsersWithLastIpAsync();
        Task<Guid?> GetUserIdByAuthTokenAsync(string authToken);
        Task SaveChangesAsync();
    }
}
