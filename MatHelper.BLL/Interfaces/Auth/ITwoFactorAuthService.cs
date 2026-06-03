using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface ITwoFactorAuthService
    {
        Task<AppTwoFactorSession> CreateTwoFactorSessionAsync(User user, DeviceInfo device, string ip, bool remember);
        Task<TwoFactorValidationResult> ValidateTwoFactorSessionAsync(string sessionKey, string code);
    }
}