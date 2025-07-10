namespace MatHelper.DAL.Models
{
    public class TaskRequestLog
    {
        public int Id { get; set; }
        public string IpAddress { get; set; } = null!;
        public DateTime RequestTime { get; set; }
        public string? UserId { get; set; }
    }
}
