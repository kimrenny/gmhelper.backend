using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IRegistrationService
    {
        Task EnsureEmailAndUsernameUniqueAsync(string email, string username);
        Task<User> BuildNewUserAsync(UserDto dto, string passwordHash);
        Task CreateInactiveInitialSessionAsync(User user, DeviceInfo device, string ip);
    }
}