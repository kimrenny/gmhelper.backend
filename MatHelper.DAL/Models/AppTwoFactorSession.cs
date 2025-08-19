using MatHelper.CORE.Models;

namespace MatHelper.DAL.Models
{
    public class AppTwoFactorSession
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public required string SessionKey { get; set; }
        public required string IpAddress { get; set; }
        public required string UserAgent { get; set; }
        public required string Platform { get; set; }
        public required Boolean Remember {  get; set; }
        public DateTime Expiration { get; set; }
        public bool IsUsed { get; set; }
    }
}
