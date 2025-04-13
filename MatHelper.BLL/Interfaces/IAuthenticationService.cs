using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IAuthenticationService
    {
        Task<bool> RegisterUserAsync(UserDto userDto, DeviceInfo deviceInfo, string ipAddress);
        Task<ConfirmTokenResult> ConfirmEmailAsync(string token);
        Task<LoginResponse> LoginUserAsync(LoginDto loginDto, DeviceInfo deviceInfo, string ipAddress);
        Task<bool> SendRecoverPasswordLinkAsync(string email);
        Task<RecoverPasswordResult> RecoverPassword(string token, string password);
    }
}