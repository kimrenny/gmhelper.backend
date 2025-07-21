namespace MatHelper.DAL.Models
{
    public class TaskRating
    {
        public int Id { get; set; }
        public required string TaskId { get; set; }
        public bool IsCorrect { get; set; }
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
