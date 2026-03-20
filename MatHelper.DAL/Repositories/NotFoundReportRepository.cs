using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;

namespace MatHelper.DAL.Repositories
{
    public class NotFoundReportRepository : INotFoundReportRepository
    {
        private readonly AppDbContext _context;

        public NotFoundReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveReportAsync(NotFoundReport report)
        {
            await _context.NotFoundReports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public IQueryable<NotFoundReport> GetReportsQuery()
        {
            return _context.NotFoundReports.AsQueryable();
        }

        public async Task ActionReportAsync(int id, ReportAction action)
        {
            if (string.IsNullOrWhiteSpace(id.ToString()))
            {
                throw new InvalidDataException("Id is null or empty");
            }

            var report = await _context.NotFoundReports.FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                throw new InvalidOperationException("Report not found.");
            }

            report.IsResolved = action == ReportAction.Resolved;

            await _context.SaveChangesAsync();
        }
    }
}
