using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace MatHelper.BLL.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ISecurityService _securityService;
        private readonly IUserRepository _userRepository;
        private readonly ILoginTokenRepository _loginTokenRepository;
        private readonly ICacheService _cache;
        private readonly ILogger _logger;

        private const string AnalyticsVersionKey = "analytics:version";
        private const string AnalyticsCachePrefix = "analytics:v";

        public AnalyticsService(ISecurityService securityService, IUserRepository userRepository, ILoginTokenRepository loginTokenRepository, ICacheService cache, ILogger<AnalyticsService> logger)
        {
            _securityService = securityService;
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<RegistrationsDto>> GetRegistrationsAsync()
        {
            try
            {
                var version = await _cache.GetVersionAsync(AnalyticsVersionKey);
                var cacheKey = $"{AnalyticsCachePrefix}{version}:registrations";

                var cached = await _cache.GetAsync<List<RegistrationsDto>>(cacheKey);
                if (cached != null)
                    return cached;

                var groupedByDate = await _userRepository.GetUserRegistrationsGroupedByDateAsync();

                await _cache.SetAsync(cacheKey, groupedByDate, null);

                return groupedByDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching registrations.");
                throw new InvalidOperationException("Could not fetch registrations.", ex);
            }
        }

        public async Task<List<CountryStatsDto>> GetUsersByCountryAsync()
        {
            try
            {
                var version = await _cache.GetVersionAsync(AnalyticsVersionKey);
                var cacheKey = $"{AnalyticsCachePrefix}{version}:users-country";

                var cached = await _cache.GetAsync<List<CountryStatsDto>>(cacheKey);
                if (cached != null)
                    return cached;

                var userIpList = await _loginTokenRepository.GetUsersWithLastIpAsync();

                if (userIpList == null || !userIpList.Any())
                {
                    _logger.LogWarning("No users found in the database.");
                    return new List<CountryStatsDto>();
                }

                var countryCounts = new ConcurrentDictionary<string, int>();
                var ipCache = new ConcurrentDictionary<string, string>();

                await Parallel.ForEachAsync(userIpList, async (user, token) =>
                {
                    if (string.IsNullOrWhiteSpace(user.IpAddress))
                        return;

                    if (!ipCache.TryGetValue(user.IpAddress, out var country))
                    {
                        country = await _securityService.GetCountryByIpAsync(user.IpAddress);
                        ipCache.TryAdd(user.IpAddress, country);
                    }

                    countryCounts.AddOrUpdate(country, 1, (_, count) => (int)(count + 1));
                });

                var result = countryCounts
                    .Select(x => new CountryStatsDto { Country = x.Key, Count = x.Value })
                    .ToList();

                await _cache.SetAsync(cacheKey, result, null);

                return result;
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
                var version = await _cache.GetVersionAsync(AnalyticsVersionKey);
                var cacheKey = $"{AnalyticsCachePrefix}{version}:role-stats";

                var cached = await _cache.GetAsync<List<RoleStatsDto>>(cacheKey);
                if (cached != null)
                    return cached;

                var users = await _userRepository.GetAllUsersAsync();

                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users provided for role stats calculation.");
                    return new List<RoleStatsDto>();
                }

                var result = users
                    .GroupBy(u => u.Role)
                    .Select(g => new RoleStatsDto { Role = g.Key, Count = g.Count() })
                    .ToList();

                await _cache.SetAsync(cacheKey, result, null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during calculating users by role.");
                throw new InvalidOperationException("Could not calculate users by role.", ex);
            }
        }

        public async Task<List<BlockStatsDto>> GetBlockStatsAsync()
        {
            try
            {
                var version = await _cache.GetVersionAsync(AnalyticsVersionKey);
                var cacheKey = $"{AnalyticsCachePrefix}{version}:block-stats";

                var cached = await _cache.GetAsync<List<BlockStatsDto>>(cacheKey);
                if (cached != null)
                    return cached;

                var users = await _userRepository.GetAllUsersAsync();

                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users provided for block stats calculation.");
                    return new List<BlockStatsDto>();
                }

                var result = users
                    .GroupBy(u => u.IsBlocked ? "Banned" : "Active")
                    .Select(g => new BlockStatsDto { Status = g.Key, Count = g.Count() })
                    .ToList();

                await _cache.SetAsync(cacheKey, result, null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during calculating block stats.");
                throw new InvalidOperationException("Could not calculate block stats.", ex);
            }
        }
    }
}