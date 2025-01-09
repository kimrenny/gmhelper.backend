using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IUserManagementService
    {
        Task<User> GetUserDetailsAsync(Guid userId);
        Task<byte[]> GetUserAvatarAsync(Guid userId);
        Task<IEnumerable<object>> GetLoggedDevicesAsync(Guid userId);
        Task SaveUserAvatarAsync(string userId, byte[] avatarBytes);
        Task UpdateUserAsync(Guid userId, UpdateUserRequest request);
    }
}