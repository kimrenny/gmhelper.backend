using MatHelper.DAL.Models;
using Microsoft.AspNetCore.Http;

namespace MatHelper.BLL.Interfaces
{
    public interface ITwoFactorService
    {
        Task<UserTwoFactor> GenerateTwoFAKeyAsync(Guid userId, string type);
        Task<bool> VerifyTwoFACodeAsync(Guid userId, string type, string code);
        string GenerateQrCode(string secret, string userEmail);
        Task<UserTwoFactor?> GetTwoFactorAsync(Guid userId, string type);
        Task RemoveTwoFactorAsync(UserTwoFactor twoFactor);
        bool VerifyTotp(string secret, string code);
    }
}