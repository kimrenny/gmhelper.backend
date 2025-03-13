using MatHelper.CORE.Models;
using MatHelper.BLL.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatHelper.BLL.Mappers
{
    public class UserMapper : IUserMapper
    {
        private readonly ILogger<UserMapper> _logger;

        public UserMapper(ILogger<UserMapper> logger)
        {
            _logger = logger;
        }

        public AdminUserDto MapToAdminUserDto(User user)
        {
            try
            {
                var loginTokens = user.LoginTokens?.Select(t => new LoginTokenDto
                {
                    Expiration = t.Expiration,
                    IpAddress = t.IpAddress,
                    IsActive = t.IsActive,
                    DeviceInfo = t.DeviceInfo != null
                        ? new DeviceInfo
                        {
                            Platform = t.DeviceInfo.Platform,
                            UserAgent = t.DeviceInfo.UserAgent,
                        }
                        : new DeviceInfo()
                }).ToList() ?? new List<LoginTokenDto>();

                return new AdminUserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    RegistrationDate = user.RegistrationDate,
                    IsBlocked = user.IsBlocked,
                    LoginTokens = loginTokens,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing user {user.Username}");
                throw;
            }
        }
    }
}
