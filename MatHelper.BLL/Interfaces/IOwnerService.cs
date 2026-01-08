using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IOwnerService
    {
        Task ChangeUserRoleAsync(Guid targetUserId, string newRole);
    }
}