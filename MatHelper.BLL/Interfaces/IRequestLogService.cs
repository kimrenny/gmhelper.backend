using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IRequestLogService
    {
        Task<List<RequestLogDto>> GetRequestStats();
        Task<List<RequestLogDetail>> GetRequestLogs();
        Task<List<AuthLog>> GetAuthLogs();
        Task<List<ErrorLog>> GetErrorLogs();
    }
}