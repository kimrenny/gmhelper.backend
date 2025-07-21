namespace MatHelper.CORE.Models
{
    public class TaskRatingDto
    {
        public required string TaskId { get; set; }
        public bool IsCorrect { get; set; }
    }
}