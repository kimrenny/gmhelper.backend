using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IEmailLoginCodeRepository
    {
        Task AddCodeAsync(EmailLoginCode code);
        Task<EmailLoginCode?> GetValidCodeAsync(Guid userId, string code);
        Task<EmailLoginCode?> GetBySessionKeyAsync(string sessionKey);
        Task SaveChangesAsync();
    }
}
