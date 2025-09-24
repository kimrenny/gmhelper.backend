using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface ITaskRatingRepository
    {
        Task AddRatingAsync(TaskRating rating);
        Task SaveChangesAsync();
    }
}
