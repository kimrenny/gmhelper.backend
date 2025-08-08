namespace MatHelper.DAL.Models
{
    public class TaskRequestLog
    {
        public int Id { get; set; }
        public required string Subject { get; set; }
        public required string TaskId { get; set; }
        public string IpAddress { get; set; } = null!;
        public DateTime RequestTime { get; set; }
        public string? UserId { get; set; }
    }
}
