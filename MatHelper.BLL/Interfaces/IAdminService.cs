using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string sortBy, bool descending, DateTime? maxRegistrationDate);
        Task ActionUserAsync(Guid userId, string action);
        Task<List<TokenDto>> GetTokensAsync();
        Task ActionTokenAsync(string token, string action);
        Task<List<RegistrationsDto>> GetRegistrationsAsync();
        Task<DashboardTokensDto> GetDashboardTokensAsync();
        Task<List<CountryStatsDto>> GetUsersByCountryAsync();
        Task<List<RoleStatsDto>> GetRoleStatsAsync();
        Task<List<BlockStatsDto>> GetBlockStatsAsync();
    }
}