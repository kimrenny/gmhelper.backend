using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MatHelper.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserRepository _userRepository;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger _logger;

        public AdminService(UserRepository userRepository, JwtOptions jwtOptions, ILogger<SecurityService> logger)
        {
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
                            Username = u.Username,
                            Email = u.Email,
                            Role = u.Role,
                            RegistrationDate = DateTime.Now,
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
    }
}