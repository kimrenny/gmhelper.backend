using MatHelper.CORE.Models;

namespace MatHelper.DAL.Models
{
    public class EmailLoginCode
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public required string Code { get; set; }
        public required string SessionKey { get; set; }
        public required string IpAddress { get; set; }
        public required string UserAgent { get; set; }
        public required string Platform { get; set; }
        public required Boolean Remember {  get; set; }
        public DateTime Expiration { get; set; }
        public bool IsUsed { get; set; }
    }
}
