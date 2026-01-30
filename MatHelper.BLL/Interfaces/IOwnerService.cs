using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IOwnerService
    {
        Task ChangeUserRoleAsync(Guid requesterUserId, Guid targetUserId, string newRole);
    }
}