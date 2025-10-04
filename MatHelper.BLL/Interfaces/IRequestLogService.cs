using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IRequestLogService
    {
        Task<CombinedRequestLogDto> GetRequestStats();
        Task<PagedResult<RequestLogDetail>> GetRequestLogs(int page, int pageSize, string sortBy, bool descending, DateTime? maxLogDate);
        Task<PagedResult<AuthLog>> GetAuthLogs(int page, int pageSize, string sortBy, bool descending, DateTime? maxLogDate);
        Task<List<ErrorLog>> GetErrorLogs();
    }
}