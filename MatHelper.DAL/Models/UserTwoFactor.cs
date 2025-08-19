namespace MatHelper.DAL.Models
{
    public class UserTwoFactor
    {
        public Guid Id {  get; set; }
        public Guid UserId { get; set; }

        public string Type { get; set; } = null!;
        public string? Secret { get; set; }

        public bool IsEnabled { get; set; }
        public bool AlwaysAsk { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
