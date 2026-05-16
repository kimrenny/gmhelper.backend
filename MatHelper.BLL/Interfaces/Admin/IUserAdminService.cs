using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IUserAdminService
    {
        Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string sortBy, bool descending, DateTime? maxRegistrationDate);
        Task ActionUserAsync(Guid userId, string action);
    }
}