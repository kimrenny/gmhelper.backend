namespace MatHelper.CORE.Models
{
    public class PasswordRecoveryDto
    {
        public string Email { get; set; }
        public string CaptchaToken { get; set; }
    }
}