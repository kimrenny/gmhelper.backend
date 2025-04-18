using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IDeviceManagementService
    {
        Task<IEnumerable<object>> GetLoggedDevicesAsync(Guid userId);
        Task<string> RemoveDeviceAsync(Guid userId, string userAgent, string platform, string ipAddress, string requestToken);
    }
}