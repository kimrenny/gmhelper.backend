namespace MatHelper.CORE.Models
{
    public class TokenDto
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
        public Guid UserId { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public string IpAddress { get; set; }
        public bool IsActive { get; set; }
    }
}