namespace MatHelper.CORE.Models
{
    public class PasswordRecoveryDto
    {
        public required string Password { get; set; }
        public required string RecoveryToken { get; set; }
        public required string CaptchaToken { get; set; }
    }
}