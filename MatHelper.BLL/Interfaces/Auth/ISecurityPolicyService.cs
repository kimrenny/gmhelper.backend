using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface ISecurityPolicyService
    {
        Task EnforceRegistrationIpLimitAsync(string ip);
        void ValidateDeviceInfo(DeviceInfo deviceInfo);
        bool IsUnfamiliar(LoginToken? lastToken, string ipAddress, string userAgent);
    }
}