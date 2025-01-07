using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IUserService
    {
        Task<bool> RegisterUserAsync(UserDto userDto);
        Task<(string AccessToken, string RefreshToken)> LoginUserAsync(LoginDto loginDto);
        Task<bool> RecoverPasswordAsync(PasswordRecoveryDto recoveryDto);
        Task SaveUserAvatarAsync(string userId, byte[] avatarBytes);
        Task<byte[]> GetUserAvatarAsync(Guid userId);
        Task<User> GetUserDetailsAsync(Guid userId);
        Task<(string AccessToken, string RefreshToken)> RefreshAccessTokenAsync(string refreshToken);
        Task<IEnumerable<object>> GetLoggedDevicesAsync(Guid userId);

    }
}