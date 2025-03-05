namespace MatHelper.DAL.Models
{
    public class RequestLogDetail
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Method { get; set; }
        public required string Path { get; set; }
        public string? UserId { get; set; }
        public string? RequestBody { get; set; }
        public int StatusCode { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public double ElapsedTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Status { get; set; }
        public string? RequestType { get; set; }
    }
}
