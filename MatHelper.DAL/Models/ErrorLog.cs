namespace MatHelper.DAL.Models
{
    public class ErrorLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
        public string? Endpoint { get; set; }
        public string? ExceptionDetails { get; set; }
    }
}
