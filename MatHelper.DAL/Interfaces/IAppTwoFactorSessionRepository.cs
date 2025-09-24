using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IAppTwoFactorSessionRepository
    {
        Task AddSessionAsync(AppTwoFactorSession session);
        Task<AppTwoFactorSession?> GetBySessionKeyAsync(string sessionKey);
        Task<List<AppTwoFactorSession>> GetAllSessionKeysAsync();
        Task SaveChangesAsync();
    }
}
