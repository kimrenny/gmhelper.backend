using MatHelper.DAL.Models;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;

namespace MatHelper.DAL.Interfaces
{
    public interface INotFoundReportRepository
    {
        Task SaveReportAsync(NotFoundReport report);
        IQueryable<NotFoundReport> GetReportsQuery();
        Task ActionReportAsync(int id, ReportAction action);
    }
}
