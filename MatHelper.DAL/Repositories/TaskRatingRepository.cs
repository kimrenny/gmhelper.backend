using MatHelper.DAL.Database;
using MatHelper.DAL.Models;

namespace MatHelper.DAL.Repositories
{
    public class TaskRatingRepository
    {
        private readonly AppDbContext _context;

        public TaskRatingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRatingAsync(TaskRating rating)
        {
            await _context.TaskRatings.AddAsync(rating);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
