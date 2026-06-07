using MatHelper.DAL.Models;
using MatHelper.CORE.Enums;

namespace MatHelper.DAL.Interfaces
{
    public interface IEmailLoginCodeRepository
    {
        Task AddCodeAsync(EmailLoginCode code);
        Task InvalidateActiveCodesByEmailAsync(string email);
        Task<ConfirmTokenResult> ConfirmByEmailAndCodeAsync(string email, string code);
        Task<EmailLoginCode?> GetValidCodeAsync(Guid userId, string code);
        Task<EmailLoginCode?> GetBySessionKeyAsync(string sessionKey);
        Task SaveChangesAsync();
    }
}
