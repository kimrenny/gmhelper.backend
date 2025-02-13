using MatHelper.CORE.Models;
using System.Threading.Tasks;

namespace MatHelper.BLL.Interfaces
{
    public interface ISecurityService
    {
        string HashPassword(string password, string salt);
        bool VerifyPassword(string password, string hash, string salt);
        string GenerateSalt();
        Task<bool> CheckSuspiciousActivityAsync(string ipAddress, string userAgent, string platform);
        Task<bool> HasAdminPermissionsAsync(Guid userId);
    }
}