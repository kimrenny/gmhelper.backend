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
        Task<bool> UpdateSwitchAsync(Guid userId, string sectionTitle, string switchLabel, bool newValue);
    }
}