namespace MatHelper.CORE.Models
{
    public class LoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string CaptchaToken { get; set; }
        public required bool Remember { get; set; }
    }
}