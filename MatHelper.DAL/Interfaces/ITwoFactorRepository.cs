using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface ITwoFactorRepository
    {
        Task AddTwoFactorAsync(UserTwoFactor twoFactor);
        Task<UserTwoFactor?> GetTwoFactorAsync(Guid userId, string type);
        Task UpdateTwoFactorModeAsync(Guid userId, string type, bool alwaysAsk);
        void Remove(UserTwoFactor twoFactor);
        Task SaveChangesAsync();
    }
}
