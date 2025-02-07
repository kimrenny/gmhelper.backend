using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<List<AdminUserDto>> GetUsersAsync();
        Task ActionUserAsync(Guid userId, string action);
    }
}