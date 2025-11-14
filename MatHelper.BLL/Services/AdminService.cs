using Azure.Core;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace MatHelper.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly ISecurityService _securityService;
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly ILoginTokenRepository _loginTokenRepository;
        private readonly IUserMapper _userMapper;
        private readonly ILogger _logger;

        private const DefaultPageNumber = 1;
        private const DefaultPageSize = 10;

        public AdminService(ISecurityService securityService, ITokenService tokenService, IUserRepository userRepository, ILoginTokenRepository loginTokenRepository, IUserMapper userMapper, ILogger<AdminService> logger)
        {
            _securityService = securityService;
            _tokenService = tokenService;
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _userMapper = userMapper;
            _logger = logger;
        }

        public async Task<AdminData> GetAdminDataAsync(Guid userId)
        {
            var users = await _userRepository.GetAllUsersAsync();

            var pagedUsers = await GetUsersAsync(DefaultPageNumber, DefaultPageSize, "RegistrationDate", false, null);
            var pagedTokens = await GetTokensAsync(DefaultPageNumber, DefaultPageSize, "Expiration", true, null);
            var registrations = await GetRegistrationsAsync();
            var dashboardTokens = await GetDashboardTokensAsync();
            var countryStats = await GetUsersByCountryAsync();

            var roleStats = CalculateRoleStats(users);
            var blockStats = CalculateBlockStats(users);

            var result = new AdminData
            {
                Users = pagedUsers,
                Tokens = pagedTokens,
                Registrations = registrations,
                DashboardTokens = dashboardTokens,
                CountryStats = countryStats,
                RoleStats = roleStats,
                BlockStats = blockStats,
            };

            return result;
        }

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string sortBy, bool descending, DateTime? maxRegistrationDate = null)
        {
            try
            {
                var usersQuery = _userRepository.GetUsersQuery();

                if (maxRegistrationDate.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.RegistrationDate <= maxRegistrationDate.Value);
                }

                int totalCount = await usersQuery.CountAsync();

                sortBy = string.IsNullOrWhiteSpace(sortBy) ? "Id" : char.ToUpper(sortBy[0]) + sortBy.Substring(1);

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

                if(users == null || users.Count == 0)
                {
                    _logger.LogWarning("No users found in the database.");
                    throw new InvalidOperationException("No users found.");
                }

                return new PagedResult<AdminUserDto>
                {
                    Items = users.Select(_userMapper.MapToAdminUserDto).ToList(),
                    TotalCount = totalCount
                };
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
                if(!Enum.TryParse<UserAction>(action, ignoreCase: true, out var parsedAction))
                {
                    throw new ArgumentException("Invalid user action.");
                }

                await _userRepository.ActionUserAsync(userId, parsedAction);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occured during action user.");
                throw new InvalidOperationException("Could not action user.", ex);
            }
        }

        public async Task<PagedResult<TokenDto>> GetTokensAsync(int page, int pageSize, string sortBy, bool descending, DateTime? maxExpirationDate)
        {
            try
            {
                var tokensQuery = _userRepository.GetTokensQuery();

                if (maxExpirationDate.HasValue)
                {
                    tokensQuery = tokensQuery.Where(t => t.Expiration <= maxExpirationDate.Value);
                }

                int totalCount = await tokensQuery.CountAsync();

                sortBy = string.IsNullOrWhiteSpace(sortBy) ? "Id" : char.ToUpper(sortBy[0]) + sortBy.Substring(1);

                tokensQuery = (sortBy, descending) switch
                {
                    ("Id", false) => tokensQuery.OrderBy(t => t.Id),
                    ("Id", true) => tokensQuery.OrderByDescending(t => t.Id),
                    ("Token", false) => tokensQuery.OrderBy(t => t.Token),
                    ("Token", true) => tokensQuery.OrderByDescending(t => t.Token),
                    ("Expiration", false) => tokensQuery.OrderBy(t => t.Expiration),
                    ("Expiration", true) => tokensQuery.OrderByDescending(t => t.Expiration),
                    ("RefreshTokenExpiration", false) => tokensQuery.OrderBy(t => t.RefreshTokenExpiration),
                    ("RefreshTokenExpiration", true) => tokensQuery.OrderByDescending(t => t.RefreshTokenExpiration),
                    ("UserId", false) => tokensQuery.OrderBy(t => t.UserId),
                    ("UserId", true) => tokensQuery.OrderByDescending(t => t.UserId),
                    ("IpAddress", false) => tokensQuery.OrderBy(t => t.IpAddress),
                    ("IpAddress", true) => tokensQuery.OrderByDescending(t => t.IpAddress),
                    ("IsActive", false) => tokensQuery.OrderBy(t => t.IsActive),
                    ("IsActive", true) => tokensQuery.OrderByDescending(t => t.IsActive),
                    _ => tokensQuery.OrderBy(t => t.Id)
                };

                var tokens = await tokensQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TokenDto
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
                                UserAgent = t.DeviceInfo.UserAgent,
                            }
                            : new DeviceInfo(),
                        IpAddress = t.IpAddress,
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                if (tokens == null || tokens.Count == 0)
                {
                    _logger.LogWarning("No tokens found in the database.");
                    throw new InvalidOperationException("No tokens found.");
                }

                return new PagedResult<TokenDto>
                {
                    Items = tokens,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching all tokens.");
                throw new InvalidOperationException("Could not fetch tokens.", ex);
            }
        }

        public async Task ActionTokenAsync(string token, string action)
        {
            try
            {
                await _loginTokenRepository.ActionTokenAsync(token, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during action user.");
                throw new InvalidOperationException("Could not action user.", ex);
            }
        }

        public async Task<List<RegistrationsDto>> GetRegistrationsAsync()
        {
            try
            {
                var groupedByDate = await _userRepository.GetUserRegistrationsGroupedByDateAsync();

                return groupedByDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching all tokens.");
                throw new InvalidOperationException("Could not fetch tokens.", ex);
            }
        }

        public async Task<DashboardTokensDto> GetDashboardTokensAsync()
        {
            try
            {
                var tokens = await _loginTokenRepository.GetDashboardTokensAsync();
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching all active users.");
                throw new InvalidOperationException("Could not fetch tokens.", ex);
            }
        }


        public async Task<List<CountryStatsDto>> GetUsersByCountryAsync()
        {
            try
            {
                var userIpList = await _loginTokenRepository.GetUsersWithLastIpAsync();

                if (userIpList == null || !userIpList.Any())
                {
                    _logger.LogWarning("No users found in the database.");
                    return new List<CountryStatsDto>();
                }

                var countryCounts = new ConcurrentDictionary<string, ushort>();
                var ipCache = new ConcurrentDictionary<string, string>();

                await Parallel.ForEachAsync(userIpList, async (user, token) =>
                {
                    if (string.IsNullOrWhiteSpace(user.IpAddress))
                    {
                        return;
                    }

                    if (!ipCache.TryGetValue(user.IpAddress, out var country))
                    {
                        country = await _securityService.GetCountryByIpAsync(user.IpAddress);
                        ipCache.TryAdd(user.IpAddress, country);
                    }

                    countryCounts.AddOrUpdate(country, 1, (_, count) => (ushort)(count + 1));
                });

                return countryCounts
                    .Select(x => new CountryStatsDto { Country = x.Key, Count = x.Value })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching users by country.");
                throw new InvalidOperationException("Could not fetch users by country.", ex);
            }
        }

        public async Task<List<RoleStatsDto>> GetRoleStatsAsync()
        {
            try
            {
                var users = await _userRepository.GetAllUsersAsync();
                return CalculateRoleStats(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching users by role.");
                throw new InvalidOperationException("Could not fetch users by role.", ex);
            }
        }

        public async Task<List<BlockStatsDto>> GetBlockStatsAsync()
        {
            try
            {
                var users = await _userRepository.GetAllUsersAsync();
                return CalculateBlockStats(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching users by role.");
                throw new InvalidOperationException("Could not fetch users by role.", ex);
            }
        }

        private List<RoleStatsDto> CalculateRoleStats(IEnumerable<User> users)
        {
            try
            {
                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users provided for role stats calculation.");
                    return new List<RoleStatsDto>();
                }

                var userRoleStats = users
                    .GroupBy(u => u.Role)
                    .Select(g => new RoleStatsDto { Role = g.Key, Count = g.Count() })
                    .ToList();

                return userRoleStats;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred during calculating users by role.");
                throw new InvalidOperationException("Could not calculate users by role.", ex);
            }
        }

        private List<BlockStatsDto> CalculateBlockStats(IEnumerable<User> users)
        {
            try
            {
                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users provided for block stats calculation.");
                    return new List<BlockStatsDto>();
                }

                var userBlockStats = users
                    .GroupBy(u => u.IsBlocked ? "Banned" : "Active")
                    .Select(g => new BlockStatsDto { Status = g.Key, Count = g.Count() })
                    .ToList();

                return userBlockStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during calculating block stats.");
                throw new InvalidOperationException("Could not calculate block stats.", ex);
            }
        }
    }
}