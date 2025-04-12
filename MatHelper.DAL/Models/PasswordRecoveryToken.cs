using MatHelper.CORE.Models;

namespace MatHelper.DAL.Models
{
    public class PasswordRecoveryToken
    {
        public int Id { get; set; }
        public required string Token { get; set; }
        public Guid UserId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsUsed { get; set; }
        
        public required User User { get; set; }
    }
}
