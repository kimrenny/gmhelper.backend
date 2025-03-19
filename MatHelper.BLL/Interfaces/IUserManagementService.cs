using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IUserManagementService
    {
        Task<UserDetails> GetUserDetailsAsync(Guid userId);
        Task<byte[]> GetUserAvatarAsync(Guid userId);
        Task<IEnumerable<object>> GetLoggedDevicesAsync(Guid userId);
        Task SaveUserAvatarAsync(string userId, byte[] avatarBytes);
        Task UpdateUserAsync(Guid userId, UpdateUserRequest request);
        Task UpdateUserLanguageAsync(Guid userId, LanguageType language);
        Task<string> RemoveDeviceAsync(Guid userId, string userAgent, string platform, string ipAddress, string requestToken);
    }
}