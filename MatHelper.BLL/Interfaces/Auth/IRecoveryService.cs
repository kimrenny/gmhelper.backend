using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IRecoveryService
    {
        Task<PasswordRecoveryToken> CreateRecoveryTokenAsync(User user);
        Task<bool> SendRecoveryEmailAsync(string email);
        Task<RecoverPasswordResult> ResetPasswordAsync(string token, string newPassword);
    }
}