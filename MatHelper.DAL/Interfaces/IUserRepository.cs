using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using System.Linq.Expressions;

namespace MatHelper.DAL.Interfaces
{
    public interface IUserRepository
    {
        void Detach<TEntity>(TEntity entity) where TEntity : class;

        Task AddUserAsync(User user);
        Task DeleteUserAsync(User user);
        Task<bool> ChangePassword(User user, string password, string salt);

        Task<User?> GetUserAsync(Expression<Func<User, bool>> predicate);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByIdAsync(Guid id);

        Task<List<User>> GetUsersByIpAsync(string ipAddress);
        Task<int> GetUserCountByIpAsync(string ipAddress);

        Task<List<User>> GetAllUsersAsync();
        Task<List<TokenDto>> GetAllTokensAsync();

        Task UpdateUserAsync(User user);
        Task ActionUserAsync(Guid id, UserAction action);

        Task<List<RegistrationsDto>> GetUserRegistrationsGroupedByDateAsync();

        Task SaveChangesAsync();
    }
}
