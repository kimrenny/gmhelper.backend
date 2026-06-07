using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface IMailService
    {
        Task SendRegistrationCodeEmailAsync(string toEmail, string code);
        Task SendConfirmationEmailAsync(string toEmail);
        Task SendPasswordRecoveryEmailAsync(string toEmail, string token);
        Task SendIpConfirmationCodeEmailAsync(string toEmail, string code);
        bool ValidateEmailFormatAsync(string email);
    }
}