using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IAuthenticationService
    {
        Task<bool> RegisterUserAsync(UserDto userDto);
        Task<(string AccessToken, string RefreshToken)> LoginUserAsync(LoginDto loginDto);
        Task<bool> RecoverPasswordAsync(PasswordRecoveryDto recoveryDto);
    }
}