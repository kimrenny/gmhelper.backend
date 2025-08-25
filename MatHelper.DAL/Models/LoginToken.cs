namespace MatHelper.CORE.Models
{
    public class LoginToken
    {
        public int Id { get; set; }
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
        public required string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public required DeviceInfo DeviceInfo { get; set; }
        public bool IsActive { get; set; }
        public required string IpAddress { get; set; }
    }
}