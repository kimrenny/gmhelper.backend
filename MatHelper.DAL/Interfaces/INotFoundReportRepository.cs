using MatHelper.DAL.Models;
using MatHelper.CORE.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface INotFoundReportRepository
    {
        Task SaveReportAsync(NotFoundReport report);
        IQueryable<NotFoundReport> GetReportsQuery();
    }
}
