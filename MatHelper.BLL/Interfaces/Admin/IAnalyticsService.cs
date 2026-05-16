using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IAnalyticsService
    {
        Task<List<RegistrationsDto>> GetRegistrationsAsync();
        Task<List<CountryStatsDto>> GetUsersByCountryAsync();
        Task<List<RoleStatsDto>> GetRoleStatsAsync();
        Task<List<BlockStatsDto>> GetBlockStatsAsync();
    }
}