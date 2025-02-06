using System;

namespace MatHelper.CORE.Models
{
    public class LoginTokenDto
    {
        public DateTime Expiration { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public string IpAddress { get; set; }
        public bool IsActive { get; set; }
    }
}