using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.DAL.Models;

namespace MatHelper.DAL.Interfaces
{
    public interface IPasswordRecoveryRepository
    {
        Task AddPasswordRecoveryTokenAsync(PasswordRecoveryToken recoveryToken);
        Task<(RecoverPasswordResult Result, User? User)> GetUserByRecoveryToken(string token);
        Task SaveChangesAsync();
    }
}
