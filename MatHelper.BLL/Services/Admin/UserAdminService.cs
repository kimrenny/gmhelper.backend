using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.BLL.Services
{
    public class UserAdminService : IUserAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserMapper _userMapper;
        private readonly ICacheService _cache;
        private readonly ILogger _logger;

        private const string UsersVersionKey = "admin:users:version";

        public UserAdminService(
            IUserRepository userRepository,
            IUserMapper userMapper,
            ICacheService cache,
            ILogger<UserAdminService> logger)
        {
            _userRepository = userRepository;
            _userMapper = userMapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            DateTime? maxRegistrationDate = null)
        {
            try
            {
                var version = await _cache.GetAsync<int?>(UsersVersionKey) ?? 1;

                sortBy = string.IsNullOrWhiteSpace(sortBy)
                    ? "Id"
                    : char.ToUpper(sortBy[0]) + sortBy.Substring(1);

                var cacheKey =
                    $"admin:users:v{version}:{page}:{pageSize}:{sortBy}:{descending}:{maxRegistrationDate}";

                var cached = await _cache.GetAsync<PagedResult<AdminUserDto>>(cacheKey);
                if (cached != null)
                    return cached;

                var usersQuery = _userRepository.GetUsersQuery();

                if (maxRegistrationDate.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.RegistrationDate <= maxRegistrationDate.Value);
                }

                int totalCount = await usersQuery.CountAsync();

                usersQuery = (sortBy, descending) switch
                {
                    ("Id", false) => usersQuery.OrderBy(u => u.Id),
                    ("Id", true) => usersQuery.OrderByDescending(u => u.Id),
                    ("Username", false) => usersQuery.OrderBy(u => u.Username),
                    ("Username", true) => usersQuery.OrderByDescending(u => u.Username),
                    ("Email", false) => usersQuery.OrderBy(u => u.Email),
                    ("Email", true) => usersQuery.OrderByDescending(u => u.Email),
                    ("Role", false) => usersQuery.OrderBy(u => u.Role),
                    ("Role", true) => usersQuery.OrderByDescending(u => u.Role),
                    ("RegistrationDate", false) => usersQuery.OrderBy(u => u.RegistrationDate),
                    ("RegistrationDate", true) => usersQuery.OrderByDescending(u => u.RegistrationDate),
                    ("IsBlocked", false) => usersQuery.OrderBy(u => u.IsBlocked),
                    ("IsBlocked", true) => usersQuery.OrderByDescending(u => u.IsBlocked),
                    _ => usersQuery.OrderBy(u => u.Id)
                };

                var users = await usersQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (users == null || users.Count == 0)
                {
                    _logger.LogWarning("No users found in the database.");
                    throw new InvalidOperationException("No users found.");
                }

                var result = new PagedResult<AdminUserDto>
                {
                    Items = users.Select(_userMapper.MapToAdminUserDto).ToList(),
                    TotalCount = totalCount
                };

                await _cache.SetAsync(cacheKey, result, null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching all users.");
                throw new InvalidOperationException("Could not fetch users.", ex);
            }
        }

        public async Task ActionUserAsync(Guid userId, string action)
        {
            try
            {
                if (!Enum.TryParse<UserAction>(action, ignoreCase: true, out var parsedAction))
                {
                    throw new ArgumentException("Invalid user action.");
                }

                await _userRepository.ActionUserAsync(userId, parsedAction);

                var version = await _cache.GetAsync<int?>(UsersVersionKey) ?? 1;
                await _cache.SetAsync(UsersVersionKey, version + 1, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during action user.");
                throw new InvalidOperationException("Could not action user.", ex);
            }
        }
    }
}