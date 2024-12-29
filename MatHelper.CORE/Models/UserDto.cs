namespace MatHelper.CORE.Models
{
    public class UserDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string IpAddress { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public string CaptchaToken { get; set; }
    }
}