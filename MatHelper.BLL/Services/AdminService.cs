using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MatHelper.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly ISecurityService _securityService;
        private readonly UserRepository _userRepository;
        private readonly LoginTokenRepository _loginTokenRepository;
        private readonly IUserMapper _userMapper;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger _logger;

        public AdminService(ISecurityService securityService, UserRepository userRepository, LoginTokenRepository loginTokenRepository, IUserMapper userMapper, JwtOptions jwtOptions, ILogger<SecurityService> logger)
        {
            _securityService = securityService;
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _userMapper = userMapper;
            _jwtOptions = jwtOptions;
            _logger = logger;
        }

        public async Task<List<AdminUserDto>> GetUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetAllUsersAsync();

                if(users == null || users.Count == 0)
                {
                    _logger.LogWarning("No users found in the database.");
                    throw new InvalidOperationException("No users found.");
                }

                return users.Select(_userMapper.MapToAdminUserDto).ToList();
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

        public async Task<List<TokenDto>> GetTokensAsync()
        {
            try
            {
                var tokens = await _userRepository.GetAllTokensAsync();

                if (tokens == null || tokens.Count == 0)
                {
                    _logger.LogWarning("No tokens found in the database.");
                    throw new InvalidOperationException("No tokens found.");
                }

                return tokens;
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

                _logger.LogInformation($"Total users: {userIpList.Count}");

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
                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users found in the database.");
                    return new List<RoleStatsDto>();
                }

                var userRoleStats = new Dictionary<string, int>();

                _logger.LogInformation($"Total users: {users.Count}");

                foreach (var user in users)
                {
                    _logger.LogInformation($"Processing user: {user.Username}");

                    var role = user.Role;

                    if (userRoleStats.ContainsKey(role))
                    {
                        userRoleStats[role]++;
                    }
                    else
                    {
                        userRoleStats[role] = 1;
                    }
                }


                return userRoleStats
                    .Select(x => new RoleStatsDto { Role = x.Key, Count = x.Value })
                    .ToList();
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
                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users found in the database.");
                    return new List<BlockStatsDto>();
                }

                var userBlockStats = new Dictionary<string, int>();

                foreach (var user in users)
                {
                    var status = user.IsBlocked ? "Banned" : "Active";

                    if (userBlockStats.ContainsKey(status))
                    {
                        userBlockStats[status]++;
                    }
                    else
                    {
                        userBlockStats[status] = 1;
                    }
                }


                return userBlockStats
                    .Select(x => new BlockStatsDto { Status = x.Key, Count = x.Value })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching users by role.");
                throw new InvalidOperationException("Could not fetch users by role.", ex);
            }
        }
    }
}