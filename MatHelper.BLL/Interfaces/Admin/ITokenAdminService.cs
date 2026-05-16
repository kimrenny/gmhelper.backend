using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface ITokenAdminService
    {
        Task<PagedResult<TokenDto>> GetTokensAsync(int page, int pageSize, string sortBy, bool descending, DateTime? maxExpirationDate);
        Task ActionTokenAsync(string token, string action);
        Task<DashboardTokensDto> GetDashboardTokensAsync();
    }
}