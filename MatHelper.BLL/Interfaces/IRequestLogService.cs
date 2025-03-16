using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IRequestLogService
    {
        Task<CombinedRequestLogDto> GetRequestStats();
        Task<List<RequestLogDetail>> GetRequestLogs();
        Task<List<AuthLog>> GetAuthLogs();
        Task<List<ErrorLog>> GetErrorLogs();
    }
}