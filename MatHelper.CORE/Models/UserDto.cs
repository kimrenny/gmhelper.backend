namespace MatHelper.CORE.Models
{
    public class UserDto
    {
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string CaptchaToken { get; set; }
    }
}