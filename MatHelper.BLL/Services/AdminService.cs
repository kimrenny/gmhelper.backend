using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace MatHelper.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly ISecurityService _securityService;
        private readonly UserRepository _userRepository;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger _logger;

        public AdminService(ISecurityService securityService, UserRepository userRepository, JwtOptions jwtOptions, ILogger<SecurityService> logger)
        {
            _securityService = securityService;
            _userRepository = userRepository;
            _jwtOptions = jwtOptions;
            _logger = logger;
        }

        public async Task<List<AdminUserDto>> GetUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetAllUsersAsync();

                if(users == null)
                {
                    _logger.LogWarning("No users found in the database.");
                    throw new InvalidOperationException("No users found.");
                }

                var usersDto = users.Select(u =>
                {
                    try
                    {
                        var loginTokens = u.LoginTokens?.Select(t =>
                        {

                            var deviceInfo = t.DeviceInfo != null
                                ? new DeviceInfo
                                {
                                    Platform = t.DeviceInfo.Platform,
                                    UserAgent = t.DeviceInfo.UserAgent
                                }
                                : new DeviceInfo();

                            return new LoginTokenDto
                            {
                                Expiration = t.Expiration,
                                IpAddress = t.IpAddress,
                                IsActive = t.IsActive,
                                DeviceInfo = deviceInfo
                            };
                        }).ToList() ?? new List<LoginTokenDto>();

                        return new AdminUserDto
                        {
                            Id = u.Id,
                            Username = u.Username,
                            Email = u.Email,
                            Role = u.Role,
                            RegistrationDate = u.RegistrationDate,
                            IsBlocked = u.IsBlocked,
                            LoginTokens = loginTokens
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing user {u.Username}");
                        throw;
                    }
                }).ToList();

                return usersDto;
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
                var users = await _userRepository.GetAllUsersAsync();
                if(users == null || !users.Any())
                {
                    _logger.LogWarning("No users found in the database.");
                    return new List<CountryStatsDto>();
                }

                var userCountryStats = new Dictionary<string, ushort>();

                _logger.LogInformation($"Total users: {users.Count}");

                foreach (var user in users)
                {
                    _logger.LogInformation($"Processing user: {user.Username}");

                    var lastActiveToken = user.LoginTokens?
                        .OrderByDescending(t => t.Expiration)
                        .FirstOrDefault();

                    if (lastActiveToken == null)
                    {
                        _logger.LogWarning($"User {user.Username} has no active tokens.");
                        continue;
                    }

                    _logger.LogInformation($"User {user.Username} has IP: {lastActiveToken.IpAddress}");

                    var country = await _securityService.GetCountryByIpAsync(lastActiveToken.IpAddress);

                    if (userCountryStats.ContainsKey(country))
                    {
                        userCountryStats[country]++;
                    }
                    else
                    {
                        userCountryStats[country] = 1;
                    }
                }


                return userCountryStats
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

                var userCountryStats = new Dictionary<string, ushort>();

                _logger.LogInformation($"Total users: {users.Count}");

                foreach (var user in users)
                {
                    _logger.LogInformation($"Processing user: {user.Username}");

                    var role = user.Role;

                    if (userCountryStats.ContainsKey(role))
                    {
                        userCountryStats[role]++;
                    }
                    else
                    {
                        userCountryStats[role] = 1;
                    }
                }


                return userCountryStats
                    .Select(x => new RoleStatsDto { Role = x.Key, Count = x.Value })
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