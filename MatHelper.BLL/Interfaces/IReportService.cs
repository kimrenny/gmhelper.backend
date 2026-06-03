using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IReportService
    {
        Task SubmitNotFoundReportAsync(NotFoundReportRequest request);
        Task<PagedResult<NotFoundReport>> GetNotFoundReportsAsync(int page, int pageSize, string sortBy, bool descending);
        Task ActionReportAsync(int reportId, string action);
    }
}