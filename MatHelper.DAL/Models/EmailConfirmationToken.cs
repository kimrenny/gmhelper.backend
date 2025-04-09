using MatHelper.CORE.Models;

namespace MatHelper.DAL.Models
{
    public class EmailConfirmationToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public Guid UserId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsUsed { get; set; }
        
        public User User { get; set; }
    }
}
