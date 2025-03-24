using MatHelper.BLL.Interfaces;
using MatHelper.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatHelper.DAL.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                            new AdminSwitch { Label = "Requests", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Tokens", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Banned", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Roles", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Country", Value = true, AdminSection = section }
                        };
                    }
                case "Users":
                    {
                        return new List<AdminSwitch>
                        {
                            new AdminSwitch { Label = "Username", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Email", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Registration", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Modal", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Modal: Token", Value = true, AdminSection = section }
                        };
                    }
                case "Tokens":
                    {
                        return new List<AdminSwitch>
                        {
                            new AdminSwitch { Label = "Token", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Expirations", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "User ID", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Modal", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Actions", Value = true, AdminSection = section }
                        };
                    }
                case "Logs":
                    {
                        return new List<AdminSwitch>
                        {
                            new AdminSwitch { Label = "Timestamp", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Duration", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Request", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "User ID", Value = true, AdminSection = section },
                            new AdminSwitch { Label = "Modal", Value = true, AdminSection = section }
                        };
                    }
                default:
                    {
                        _logger.LogError($"Error occured during GenerateDefaultSwitches, switch block, section: {section}");
                        throw new InvalidDataException();
                    }
            }
        }

        public async Task<bool> UpdateSwitchAsync(Guid userId, int sectionId, string switchLabel, bool newValue)
        {
            try
            {
                return await _adminSettingsRepository.UpdateSwitchAsync(userId, sectionId, switchLabel, newValue);
            }
            catch (Exception ex) 
            {
                _logger.LogError($"Unknown error occured during request: {ex}");
                return false;
            }
        }
    }

}
