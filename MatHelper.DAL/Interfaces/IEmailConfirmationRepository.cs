using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IEmailConfirmationRepository
    {
        Task<EmailConfirmationToken?> GetTokenByUserIdAsync(Guid userId);
        Task AddEmailConfirmationTokenAsync(EmailConfirmationToken emailConfirmationToken);
        Task<(ConfirmTokenResult Result, User? User)> ConfirmUserByTokenAsync(string token);
        Task SaveChangesAsync();
    }
}
