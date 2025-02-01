namespace MatHelper.CORE.Models
{
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public string IpAddress { get; set; }
        public string CaptchaToken { get; set; }
        public bool Remember { get; set; }
    }
}