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
        public string? StatusCode { get; set; }
    }
}
