namespace MatHelper.DAL.Models
{
    public class AuthLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
    }
}
