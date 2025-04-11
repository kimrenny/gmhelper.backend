namespace MatHelper.CORE.Models
{
    public class ConfirmEmailDto
    {
        public required string Token { get; set; }
        public required string CaptchaToken { get; set; }
    }
}