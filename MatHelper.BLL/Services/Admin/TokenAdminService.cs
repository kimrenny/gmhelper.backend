using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.BLL.Services
{
    public class TokenAdminService : ITokenAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginTokenRepository _loginTokenRepository;
        private readonly ICacheService _cache;
        private readonly ILogger _logger;

        private const string TokensVersionKey = "tokens:admin:version";

        public TokenAdminService(
            IUserRepository userRepository,
            ILoginTokenRepository loginTokenRepository,
            ICacheService cache,
            ILogger<TokenAdminService> logger)
        {
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PagedResult<TokenDto>> GetTokensAsync(
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            DateTime? maxExpirationDate)
        {
            try
            {
                var version = await _cache.GetAsync<int?>(TokensVersionKey) ?? 1;

                sortBy = string.IsNullOrWhiteSpace(sortBy)
                    ? "Id"
                    : char.ToUpper(sortBy[0]) + sortBy.Substring(1);

                var cacheKey =
                    $"tokens:admin:v{version}:{page}:{pageSize}:{sortBy}:{descending}:{maxExpirationDate}";

                var cached = await _cache.GetAsync<PagedResult<TokenDto>>(cacheKey);
                if (cached != null)
                    return cached;

                var tokensQuery = _userRepository.GetTokensQuery();

                if (maxExpirationDate.HasValue)
                {
                    tokensQuery = tokensQuery.Where(t => t.Expiration <= maxExpirationDate.Value);
                }

                int totalCount = await tokensQuery.CountAsync();

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

                var result = new PagedResult<TokenDto>
                {
                    Items = tokens,
                    TotalCount = totalCount
                };

                await _cache.SetAsync(cacheKey, result, null);

                return result;
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

                var version = await _cache.GetAsync<int?>(TokensVersionKey) ?? 1;
                await _cache.SetAsync(TokensVersionKey, version + 1, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during action token.");
                throw new InvalidOperationException("Could not action token.", ex);
            }
        }

        public async Task<DashboardTokensDto> GetDashboardTokensAsync()
        {
            try
            {
                var cacheKey = "tokens:dashboard";

                var cached = await _cache.GetAsync<DashboardTokensDto>(cacheKey);
                if (cached != null)
                    return cached;

                var tokens = await _loginTokenRepository.GetDashboardTokensAsync();

                await _cache.SetAsync(cacheKey, tokens, null);

                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during fetching dashboard tokens.");
                throw new InvalidOperationException("Could not fetch tokens.", ex);
            }
        }
    }
}