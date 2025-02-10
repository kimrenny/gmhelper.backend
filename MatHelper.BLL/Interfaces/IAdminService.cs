using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<List<AdminUserDto>> GetUsersAsync();
        Task ActionUserAsync(Guid userId, string action);
        Task<List<TokenDto>> GetTokensAsync();
        Task ActionTokenAsync(string token, string action);
        Task<List<RegistrationsDto>> GetRegistrationsAsync();
    }
}