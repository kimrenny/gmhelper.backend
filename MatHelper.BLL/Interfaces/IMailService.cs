using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IMailService
    {
        Task SendConfirmationEmailAsync(string toEmail, string token);
        Task SendPasswordRecoveryEmailAsync(string toEmail, string token);
        Task SendIpConfirmationCodeEmailAsync(string toEmail, string code);
    }
}