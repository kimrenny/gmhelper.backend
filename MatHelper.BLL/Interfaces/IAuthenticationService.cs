using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IAuthenticationService
    {
        Task<bool> RegisterUserAsync(UserDto userDto, DeviceInfo deviceInfo, string ipAddress);
        Task<(string AccessToken, string RefreshToken)> LoginUserAsync(LoginDto loginDto, DeviceInfo deviceInfo, string ipAddress);
        Task<bool> RecoverPasswordAsync(PasswordRecoveryDto recoveryDto);
    }
}