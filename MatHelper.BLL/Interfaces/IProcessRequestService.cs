using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IProcessRequestService
    {
        public (DeviceInfo, string?) GetRequestInfo();
    }
}