using System.Text.Json;
using MatHelper.BLL.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MatHelper.BLL.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;

        private static bool _disabled;
        private static DateTime _disabledUntil = DateTime.MinValue;
        private static readonly TimeSpan _cooldown = TimeSpan.FromSeconds(30);

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (IsDisabled())
                return default;

            try
            {
                var value = await _cache.GetStringAsync(key);

                if (string.IsNullOrEmpty(value))
                    return default;

                return JsonSerializer.Deserialize<T>(value);
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (IsDisabled())
                return;

            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
                options.AbsoluteExpirationRelativeToNow = expiration;

            var json = JsonSerializer.Serialize(value);

            try
            {
                await _cache.SetStringAsync(key, json, options);
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (IsDisabled())
                return;

            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
            }
        }

        public async Task<int> GetVersionAsync(string key)
        {
            var version = await GetAsync<int?>(key);
            return version ?? 1;
        }

        public async Task<int> IncrementVersionAsync(string key)
        {
            var version = await GetVersionAsync(key);
            version++;

            await SetAsync(key, version, null);

            return version;
        }

        private bool IsDisabled()
        {
            if (!_disabled)
                return false;

            if (DateTime.UtcNow > _disabledUntil)
            {
                _disabled = false;
                return false;
            }

            return true;
        }

        private void HandleFailure(Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable. Temporarily disabling cache.");

            _disabled = true;
            _disabledUntil = DateTime.UtcNow.Add(_cooldown);
        }
    }
}