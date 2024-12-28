using System;

namespace MatHelper.CORE.Models
{
    public class LoginToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public bool IsActive { get; set; }
    }
}