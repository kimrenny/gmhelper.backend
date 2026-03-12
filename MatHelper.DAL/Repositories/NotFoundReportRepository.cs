using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using MatHelper.CORE.Models;

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
    }
}
