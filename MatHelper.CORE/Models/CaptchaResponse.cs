namespace MatHelper.CORE.Models
{
    public class CaptchaResponse
    {
        public bool Success { get; set; }
        public string? ChallengeTs { get; set; }
        public string? Hostname { get; set; }
        public List<string>? ErrorCodes { get; set; }
    }
}
