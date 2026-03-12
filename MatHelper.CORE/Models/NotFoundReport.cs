namespace MatHelper.CORE.Models
{
    public class NotFoundReport
    {
        public int Id { get; set; }

        public string Report { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string? UserAgent { get; set; }

        public string? Referrer { get; set; }

        public string? Language { get; set; }

        public int? ScreenWidth { get; set; }
        public int? ScreenHeight { get; set; }

        public int? ViewportWidth { get; set; }
        public int? ViewportHeight { get; set; }

        public DateTime? ClientTimestamp { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
