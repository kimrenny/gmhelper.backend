using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IIpLoginAttemptRepository
    {
        Task<IpLoginAttempt?> GetByIpAsync(string ipAddress);
        Task AddAsync(IpLoginAttempt attempt);
        Task SaveChangesAsync();
    }
}
