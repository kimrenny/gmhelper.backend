using MatHelper.CORE.Models;
using MatHelper.DAL.Database;
using Microsoft.EntityFrameworkCore;

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

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidDataException("email is null or empty");
            }

            var user = await _context.Users.Include(u => u.LoginTokens).FirstOrDefaultAsync(u => u.Email == email);
            if (user != null && user.IsBlocked)
            {
                throw new InvalidOperationException("User is blocked.");
            }
            return user;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidDataException("username is null or empty");
            }

            var user = await _context.Users.Include(u => u.LoginTokens).FirstOrDefaultAsync(u => u.Username == username);
            if (user != null && user.IsBlocked)
            {
                throw new InvalidOperationException("User is blocked.");
            }
            return user;
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            if (string.IsNullOrWhiteSpace(id.ToString()))
            {
                throw new InvalidDataException("id is null or empty");
            }

            var user = await _context.Users.Include(u => u.LoginTokens).FirstOrDefaultAsync(u => u.Id == id);
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
            var query = _context.LoginTokens.Where(t => t.RefreshToken == refreshToken);
            return await query.FirstOrDefaultAsync();
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

        public async Task<List<LoginToken>> GetAllTokensAsync()
        {
            var tokens = await _context.LoginTokens.ToListAsync();
            return tokens ?? new List<LoginToken>();
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

        public async Task<int> GetActiveTokensAsync()
        {
            var activeUserTokens = await _context.Users
                .Where(u => u.LoginTokens!.Any())
                .SelectMany(u => u.LoginTokens!)
                .Where(t => t.Expiration > DateTime.UtcNow && t.IsActive)
                .GroupBy(t => t.UserId)
                .Where(g => g.Count() > 0)
                .Select(g => g.OrderByDescending(t => t.Expiration).First())
                .ToListAsync();

            return activeUserTokens.Count;
        }

        public async Task<int> GetTotalTokensAsync()
        {
            var totalLoginTokens = await _context.Users
                .Where(u => u.LoginTokens!.Any())
                .SumAsync(u => u.LoginTokens!.Count);

            return totalLoginTokens;
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