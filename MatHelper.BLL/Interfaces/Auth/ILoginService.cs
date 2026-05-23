using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface ILoginService
    {
        Task<User> ValidateUserCredentialsAsync(string email, string password);
        Task EnsureUserCanLoginAsync(User user);
        Task<LoginToken> IssueLoginTokenAsync(User user, DeviceInfo device, string ip, bool remember);
        Task CleanupUserSessionsAsync(User user, DeviceInfo device, string ip);
        Task ApplySessionLimitsAsync(User user);
    }
}