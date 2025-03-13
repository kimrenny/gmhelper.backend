using MatHelper.BLL.Interfaces;
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
        private readonly IUserMapper _userMapper;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger _logger;

        public AdminService(ISecurityService securityService, UserRepository userRepository, IUserMapper userMapper, JwtOptions jwtOptions, ILogger<SecurityService> logger)
        {
            _securityService = securityService;
            _userRepository = userRepository;
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
                await _userRepository.ActionUserAsync(userId, action);
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

                if (tokens == null)
                {
                    _logger.LogWarning("No users found in the database.");
                    throw new InvalidOperationException("No users found.");
                }

                var tokensDto = tokens.Select(t =>
                {
                       var deviceInfo = t.DeviceInfo != null
                         ? new DeviceInfo
                         {
                                Platform = t.DeviceInfo.Platform,
                                UserAgent = t.DeviceInfo.UserAgent
                         }
                         : new DeviceInfo();

                    return new TokenDto
                    {
                        Id = t.Id,
                        Token = t.Token,
                        Expiration = t.Expiration,
                        RefreshTokenExpiration = t.RefreshTokenExpiration,
                        UserId = t.UserId,
                        DeviceInfo = deviceInfo,
                        IpAddress = t.IpAddress,
                        IsActive = t.IsActive,
                    };
                }).ToList() ?? new List<TokenDto>();

                return tokensDto;
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
                await _userRepository.ActionTokenAsync(token, action);
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
                var activeTokens = await _userRepository.GetActiveTokensAsync();
                var totalTokens = await _userRepository.GetTotalTokensAsync();
                var activeAdminTokens = await _userRepository.GetActiveAdminTokensAsync();
                var totalAdminTokens = await _userRepository.GetTotalAdminTokensAsync();

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
                _logger.LogError(ex, "An error occured during fetching all active users.");
                throw new InvalidOperationException("Could not fetch tokens.", ex);
            }
        }


        public async Task<List<CountryStatsDto>> GetUsersByCountryAsync()
        {
            try
            {
                var userIpList = await _userRepository.GetUsersWithLastIpAsync();

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