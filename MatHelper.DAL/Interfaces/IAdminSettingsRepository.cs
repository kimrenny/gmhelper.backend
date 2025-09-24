using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IAdminSettingsRepository
    {
        Task<AdminSettings?> GetByUserIdAsync(Guid userId);
        Task CreateAsync(AdminSettings adminSettings);
        Task<bool> UpdateSwitchAsync(Guid userId, string sectionTitle, string switchLabel, bool newValue);
        Task SaveChangesAsync();
    }
}
