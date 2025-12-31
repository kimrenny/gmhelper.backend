namespace MatHelper.DAL.Models
{
    public class IpLoginAttempt
    {
        public int Id { get; set; }
        public string IpAddress { get; set; } = null!;
        public int FailedCount { get; set; }
        public DateTime? BlockedUntil { get; set; }
        public DateTime LastAttemptAt { get; set; }
    }
}