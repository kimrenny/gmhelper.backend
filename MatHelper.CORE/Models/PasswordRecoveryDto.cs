namespace MatHelper.CORE.Models
{
    public class PasswordRecoveryDto
    {
        public required string Email { get; set; }
        public required string CaptchaToken { get; set; }
    }
}