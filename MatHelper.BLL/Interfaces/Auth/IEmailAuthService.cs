using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IEmailAuthService
    {
        Task<EmailLoginCode> CreateEmailLoginCodeAsync(User user, DeviceInfo device, string ip, bool remember);
        Task<EmailConfirmationToken> CreateEmailConfirmationTokenAsync(User user);
        Task<ConfirmTokenResult> ConfirmEmailAsync(string token);
    }
}