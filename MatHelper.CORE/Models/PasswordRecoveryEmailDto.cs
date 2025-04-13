namespace MatHelper.CORE.Models
{
    public class PasswordRecoveryEmailDto
    {
        public required string Email { get; set; }
        public required string CaptchaToken { get; set; }
    }
}