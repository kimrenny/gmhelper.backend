using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatHelper.DAL.Models;
using MatHelper.CORE.Models;
using System.Security.Claims;
using MatHelper.CORE.Enums;

namespace MatHelper.DAL.Repositories
{
    public class PasswordRecoveryRepository
    {
        private readonly AppDbContext _context;

        public PasswordRecoveryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddPasswordRecoveryTokenAsync(PasswordRecoveryToken recoveryToken)
        {
            await _context.PasswordRecoveryTokens.AddAsync(recoveryToken);
            await _context.SaveChangesAsync();
        }

        public async Task<(RecoverPasswordResult Result, User? User)> GetUserByRecoveryToken(string token)
        {
            //var sw = Stopwatch.StartNew();

            var recoveryToken = await _context.PasswordRecoveryTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            //sw.Stop();

            if (recoveryToken == null)
            {
                return (RecoverPasswordResult.TokenNotFound, null);
            }

            if (recoveryToken.IsUsed)
            {
                return (RecoverPasswordResult.TokenUsed, null);
            }

            if (recoveryToken.ExpirationDate <= DateTime.UtcNow)
            {
                return (RecoverPasswordResult.TokenExpired, null);
            }

            recoveryToken.IsUsed = true;
            await _context.SaveChangesAsync();

            return (RecoverPasswordResult.Success, recoveryToken.User);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
