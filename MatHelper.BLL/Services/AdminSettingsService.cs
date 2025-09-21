using MatHelper.BLL.Interfaces;
using MatHelper.DAL.Repositories;
using MatHelper.DAL.Models;
using Microsoft.Extensions.Logging;

namespace MatHelper.BLL.Services
{
    public class AdminSettingsService : IAdminSettingsService
    {
        private readonly AdminSettingsRepository _adminSettingsRepository;
        private readonly UserRepository _userRepository;
        private readonly ILogger _logger;

        public AdminSettingsService(AdminSettingsRepository adminSettingsRepository, UserRepository userRepository, ILogger<AdminSettingsService> logger)
        {
            _adminSettingsRepository = adminSettingsRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool[][]> GetOrCreateAdminSettingsAsync(Guid userId)
        {
            var adminSettings = await _adminSettingsRepository.GetByUserIdAsync(userId);

            if (adminSettings == null)
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new InvalidDataException("User not found.");
                }

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
                {
                    section.Switches = GenerateDefaultSwitches(section);
                }

                adminSettings.Sections = sections;
                await _adminSettingsRepository.CreateAsync(adminSettings);
            }

            return adminSettings.Sections
                .OrderBy(s => s.Id)
                .Select(s => s.Switches.OrderBy(sw => sw.Id).Select(sw => sw.Value).ToArray())
                .ToArray();
        }

        private List<AdminSwitch> GenerateDefaultSwitches(AdminSection section)
        {
            switch (section.Title)
            {
                case "Dashboard":
                    {

                        return new List<AdminSwitch>
                        {
                            new AdminSwitch { Label = "requests", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "tokens", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "banned", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "roles", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "country", Value = true, AdminSection = section }
                        };
                    }
                case "Users":
                    {
                        return new List<AdminSwitch>
                        {
                            new AdminSwitch { Label = "username", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "email", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "registration", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "modal", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "modaltoken", Value = true, AdminSection = section }
                        };
                    }
                case "Tokens":
                    {
                        return new List<AdminSwitch>
                        {
                            new AdminSwitch { Label = "token", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "expirations", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "userid", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "modal", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "actions", Value = true, AdminSection = section }
                        };
                    }
                case "Logs":
                    {
                        return new List<AdminSwitch>
                        {
                            new AdminSwitch { Label = "timestamp", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "duration", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "request", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "userid", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "modal", Value = true, AdminSection = section }
                        };
                    }
                default:
                    {
                        _logger.LogError($"Error occured during GenerateDefaultSwitches, switch block, section: {section}");
                        throw new InvalidDataException();
                    }
            }
        }

        public async Task<bool> UpdateSwitchAsync(Guid userId, string sectionTitle, string switchLabel, bool newValue)
        {
            try
            {
                var formattedSection = sectionTitle.ToLowerInvariant();
                var formattedLabel =switchLabel.ToLowerInvariant();

                return await _adminSettingsRepository.UpdateSwitchAsync(userId, formattedSection, formattedLabel, newValue);
            }
            catch (Exception ex) 
            {
                _logger.LogError($"Unknown error occured during request: {ex}");
                return false;
            }
        }
    }

}
