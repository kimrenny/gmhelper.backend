using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.DAL.Database;
using MatHelper.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;

namespace MatHelper.DAL.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task AddEmailConfirmationTokenAsync(EmailConfirmationToken emailConfirmationToken)
        {
            await _context.EmailConfirmationTokens.AddAsync(emailConfirmationToken);
            await _context.SaveChangesAsync();
        }

        public async Task AddPasswordRecoveryTokenAsync(PasswordRecoveryToken recoveryToken)
        {
            await _context.PasswordRecoveryTokens.AddAsync(recoveryToken);
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

            if(confirmationToken.ExpirationDate <= DateTime.UtcNow)
            {
                return (ConfirmTokenResult.TokenExpired, confirmationToken.User);
            }

            confirmationToken.IsUsed = true;
            confirmationToken.User.IsActive = true;

            await _context.SaveChangesAsync();
            return (ConfirmTokenResult.Success, null);
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

        public async Task<bool> ChangePassword(User user, string password, string salt)
        {
            try
            {
                user.PasswordHash = password;
                user.PasswordSalt = salt;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


            public async Task<User?> GetUserAsync(Expression<Func<User, bool>> predicate)
        {
            return await _context.Users.Include(u => u.LoginTokens).FirstOrDefaultAsync(predicate);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            ValidateEmailOrUsername(email, "email");

            var user = await GetUserAsync(u => u.Email == email);
            if (user != null && user.IsBlocked)
            {
                throw new InvalidOperationException("User is blocked.");
            }
            return user;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            ValidateEmailOrUsername(username, "username");

            var user = await GetUserAsync(u => u.Username == username);
            if (user != null && user.IsBlocked)
            {
                throw new InvalidOperationException("User is blocked.");
            }
            return user;
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new InvalidDataException("id is null or empty");
            }

            var user = await GetUserAsync(u => u.Id == id);
            if (user != null && user.IsBlocked)
            {
                throw new InvalidOperationException("User is blocked.");
            }
            return user;
        }

        public async Task<List<User>> GetUsersByIpAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new InvalidDataException("ipAddress is null or empty");
            }

            return await _context.Users.Include(u => u.LoginTokens).Where(u => u.LoginTokens != null && u.LoginTokens.Any(t => t.IpAddress == ipAddress)).ToListAsync();
        }

        public async Task<int> GetUserCountByIpAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return 0;
            }

            return await _context.LoginTokens.Where(t => t.IpAddress == ipAddress).Select(t => t.UserId).Distinct().CountAsync();
        }

        public async Task<LoginToken?> GetLoginTokenByRefreshTokenAsync(string refreshToken)
        {
            return await _context.LoginTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
        }

        public async Task<LoginToken?> GetLoginTokenAsync(string token)
        {
            return await _context.LoginTokens.Where(t => t.Token == token).FirstOrDefaultAsync();
        }

        public async Task RemoveLoginTokenAsync(LoginToken token)
        {
            _context.LoginTokens.Remove(token);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LoginToken>> GetAllLoginTokensAsync()
        {
            return await _context.LoginTokens.Include(t => t.User).ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return users ?? new List<User>();
        }

        public async Task<List<TokenDto>> GetAllTokensAsync()
        {
            try
            {
                var tokens = await _context.LoginTokens
                    .Include(t => t.DeviceInfo)
                    .ToListAsync();

                if (tokens == null || !tokens.Any())
                {
                    throw new InvalidOperationException("No tokens found.");
                }

                return tokens.Select(t => new TokenDto
                {
                    Id = t.Id,
                    Token = t.Token,
                    Expiration = t.Expiration,
                    RefreshTokenExpiration = t.RefreshTokenExpiration,
                    UserId = t.UserId,
                    DeviceInfo = t.DeviceInfo != null
                        ? new DeviceInfo
                        {
                            Platform = t.DeviceInfo.Platform,
                            UserAgent = t.DeviceInfo.UserAgent
                        }
                        : new DeviceInfo(),
                    IpAddress = t.IpAddress,
                    IsActive = t.IsActive,
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not fetch tokens.", ex);
            }
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task ActionUserAsync(Guid id, string action)
        {
            if (string.IsNullOrWhiteSpace(id.ToString()))
            {
                throw new InvalidDataException("Id is null or empty");
            }

            var user = await _context.Users.Include(u => u.LoginTokens).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (action == "ban")
            {
                user.IsBlocked = true;
            }
            else if (action == "unban")
            {
                user.IsBlocked = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ActionTokenAsync(string authToken, string action)
        {
            if (string.IsNullOrWhiteSpace(authToken))
            {
                throw new InvalidDataException("Id is null or empty");
            }

            var token = await _context.LoginTokens.FirstOrDefaultAsync(u => u.Token == authToken);

            if (token == null)
            {
                throw new InvalidOperationException("Token not found.");
            }

            if (action == "disable")
            {
                token.IsActive = false;
            }
            else if (action == "activate")
            {
                token.IsActive = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<RegistrationsDto>> GetUserRegistrationsGroupedByDateAsync()
        {
            var users = await _context.Users
                                      .Select(u => u.RegistrationDate)
                                      .ToListAsync();

            var groupedByDate = users
                .GroupBy(date => date.Date)
                .Select(group => new RegistrationsDto
                { 
                    Date = DateOnly.FromDateTime(group.Key), 
                    Registrations = (ushort)group.Count() 
                })
                .ToList();

            return groupedByDate;
        }

        public async Task<DashboardTokensDto> GetDashboardTokensAsync()
        {
            try
            {
                var tokensData = await _context.Users
                    .Where(u => u.LoginTokens!.Any())
                    .Select(u => new
                    {
                        u.Id,
                        u.Role,
                        ActiveTokens = u.LoginTokens!.Count(t => t.Expiration > DateTime.UtcNow && t.IsActive),
                        TotalTokens = u.LoginTokens!.Count()
                    })
                    .ToListAsync();

                var activeTokens = tokensData.Sum(x => x.ActiveTokens);
                var totalTokens = tokensData.Sum(x => x.TotalTokens);
                var activeAdminTokens = tokensData.Where(u => u.Role.ToLower() == "admin" || u.Role.ToLower() == "owner")
                                                  .Sum(u => u.ActiveTokens);
                var totalAdminTokens = tokensData.Where(u => u.Role.ToLower() == "admin" || u.Role.ToLower() == "owner")
                                                 .Sum(u => u.TotalTokens);

                return new DashboardTokensDto
                {
                    ActiveTokens = activeTokens,
                    TotalTokens = totalTokens,
                    ActiveAdminTokens = activeAdminTokens,
                    TotalAdminTokens = totalAdminTokens
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not fetch tokens.", ex);
            }
        }

        public async Task<List<UserIp>> GetUsersWithLastIpAsync()
        {
            return await _context.Users
                .Where(u => u.LoginTokens != null && u.LoginTokens.Any())
                .Select(u => new UserIp 
                { 
                    Id = u.Id, 
                    IpAddress = u.LoginTokens!
                        .OrderByDescending(t => t.Expiration)
                        .Select(t => t.IpAddress)
                        .FirstOrDefault() ?? "Unknown" 
                })
                .AsNoTracking()
                .ToListAsync();
        }

        private void ValidateEmailOrUsername(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException($"{fieldName} is null or empty.");
            }
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException ex)
            {
                foreach(var entry in ex.Entries)
                {
                    if(entry.Entity is User)
                    {
                        var proposedValues = entry.CurrentValues;
                        var databaseValues = entry.GetDatabaseValues();
                    }
                }

                throw new Exception("Concurrent exception due updating the data");
            }
        }
    }
}