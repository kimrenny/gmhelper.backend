using MatHelper.BLL.Interfaces;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;

namespace MatHelper.BLL.Services
{
    public class AdminSettingsService : IAdminSettingsService
    {
        private readonly IAdminSettingsRepository _adminSettingsRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cache;
        private readonly ILogger _logger;

        private const string CacheVersionKey = "adminsettings:version";
        private const string CacheBaseKey = "adminsettings";

        public AdminSettingsService(
            IAdminSettingsRepository adminSettingsRepository,
            IUserRepository userRepository,
            ICacheService cache,
            ILogger<AdminSettingsService> logger)
        {
            _adminSettingsRepository = adminSettingsRepository;
            _userRepository = userRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool[][]> GetOrCreateAdminSettingsAsync(Guid userId)
        {
            try
            {
                var version = await _cache.GetAsync<int?>(CacheVersionKey) ?? 1;
                var cacheKey = $"{CacheBaseKey}:v{version}:{userId}";

                var cached = await _cache.GetAsync<bool[][]>(cacheKey);
                if (cached != null)
                    return cached;

                var adminSettings = await _adminSettingsRepository.GetByUserIdAsync(userId);

                if (adminSettings == null)
                {
                    var user = await _userRepository.GetUserByIdAsync(userId);
                    if (user == null)
                        throw new InvalidDataException("User not found.");

                    adminSettings = new AdminSettings
                    {
                        UserId = userId,
                        User = user,
                        Sections = new List<AdminSection>()
                    };

                    var sections = new List<AdminSection>
                    {
                        new AdminSection { Title = "Dashboard", AdminSettings = adminSettings },
                        new AdminSection { Title = "Users", AdminSettings = adminSettings },
                        new AdminSection { Title = "Tokens", AdminSettings = adminSettings },
                        new AdminSection { Title = "Logs", AdminSettings = adminSettings }
                    };

                    foreach (var section in sections)
                        section.Switches = GenerateDefaultSwitches(section);

                    adminSettings.Sections = sections;

                    await _adminSettingsRepository.CreateAsync(adminSettings);

                    var currentVersion = await _cache.GetAsync<int?>(CacheVersionKey) ?? 1;
                    await _cache.SetAsync(CacheVersionKey, currentVersion + 1, null);
                }

                var result = adminSettings.Sections
                    .OrderBy(s => s.Id)
                    .Select(s => s.Switches
                        .OrderBy(sw => sw.Id)
                        .Select(sw => sw.Value)
                        .ToArray())
                    .ToArray();

                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GetOrCreateAdminSettingsAsync");
                throw;
            }
        }

        public async Task<bool> UpdateSwitchAsync(Guid userId, string sectionTitle, string switchLabel, bool newValue)
        {
            try
            {
                var formattedSection = sectionTitle.ToLowerInvariant();
                var formattedLabel = switchLabel.ToLowerInvariant();

                var result = await _adminSettingsRepository.UpdateSwitchAsync(
                    userId,
                    formattedSection,
                    formattedLabel,
                    newValue);

                var version = await _cache.GetAsync<int?>(CacheVersionKey) ?? 1;
                await _cache.SetAsync(CacheVersionKey, version + 1, null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error occurred during UpdateSwitchAsync");
                return false;
            }
        }

        private List<AdminSwitch> GenerateDefaultSwitches(AdminSection section)
        {
            switch (section.Title)
            {
                case "Dashboard":
                    return new List<AdminSwitch>
                    {
                        new AdminSwitch { Label = "requests", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "tokens", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "banned", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "roles", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "country", Value = true, AdminSection = section }
                    };

                case "Users":
                    return new List<AdminSwitch>
                    {
                        new AdminSwitch { Label = "username", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "email", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "registration", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "modal", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "modaltoken", Value = true, AdminSection = section }
                    };

                case "Tokens":
                    return new List<AdminSwitch>
                    {
                        new AdminSwitch { Label = "token", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "expirations", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "userid", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "modal", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "actions", Value = true, AdminSection = section }
                    };

                case "Logs":
                    return new List<AdminSwitch>
                    {
                        new AdminSwitch { Label = "timestamp", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "duration", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "request", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "userid", Value = true, AdminSection = section },
                        new AdminSwitch { Label = "modal", Value = true, AdminSection = section }
                    };

                default:
                    _logger.LogError("Invalid section: {section}", section.Title);
                    throw new InvalidDataException();
            }
        }
    }
}