namespace MatHelper.CORE.Models
{
    public class ClientInfoDto
    {
        public string Url { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public string? Referrer { get; set; }
        public string? Language { get; set; }
        public SizeDto? Screen { get; set; }
        public SizeDto? Viewport { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
