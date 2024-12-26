using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IUserService
    {
        Task<bool> RegisterUserAsync(UserDto userDto);
        Task<string> LoginUserAsync(LoginDto loginDto);
        Task<bool> RecoverPasswordAsync(PasswordRecoveryDto recoveryDto);
        Task SaveUserAvatarAsync(string userId, byte[] avatarBytes);
        Task<byte[]> GetUserAvatarAsync(string userId);
        Task<User> GetUserDetailsAsync(string userId);
        Task<string> RefreshAccessTokenAsync(string refreshToken);
    }
}