using MatHelper.CORE.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MatHelper.DAL.Models;

namespace MatHelper.BLL.Interfaces
{
    public interface IAdminSettingsService
    {
        Task<bool[][]> GetOrCreateAdminSettingsAsync(Guid userId);
        Task<bool> UpdateSwitchAsync(Guid userId, int sectionId, string switchLabel, bool newValue);
    }
}