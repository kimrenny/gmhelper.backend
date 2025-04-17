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
    public class EmailConfirmationRepository
    {
        private readonly AppDbContext _context;

        public EmailConfirmationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddEmailConfirmationTokenAsync(EmailConfirmationToken emailConfirmationToken)
        {
            await _context.EmailConfirmationTokens.AddAsync(emailConfirmationToken);
            await _context.SaveChangesAsync();
        }

        public async Task<(ConfirmTokenResult Result, User? User)> ConfirmUserByTokenAsync(string token)
        {
            var confirmationToken = await _context.EmailConfirmationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (confirmationToken == null)
            {
                return (ConfirmTokenResult.TokenNotFound, null);
            }

            if (confirmationToken.IsUsed)
            {
                return (ConfirmTokenResult.TokenUsed, null);
            }

            if (confirmationToken.ExpirationDate <= DateTime.UtcNow)
            {
                return (ConfirmTokenResult.TokenExpired, confirmationToken.User);
            }

            confirmationToken.IsUsed = true;
            confirmationToken.User.IsActive = true;

            await _context.SaveChangesAsync();
            return (ConfirmTokenResult.Success, null);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
