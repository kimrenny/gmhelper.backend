using MatHelper.DAL.Repositories;
using MatHelper.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace MatHelper.BLL.Services
{
    public class DeviceManagementService : IDeviceManagementService
    {
        private readonly UserRepository _userRepository;
        private readonly ILogger _logger;

        public DeviceManagementService(UserRepository userRepository, ILogger<DeviceManagementService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<object>> GetLoggedDevicesAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            return user.LoginTokens
                !.Where(t => t.IsActive)
                .Select(t => new
                {
                    Platform = t.DeviceInfo.Platform,
                    UserAgent = t.DeviceInfo.UserAgent,
                    IpAddress = t.IpAddress,
                    AuthorizationDate = t.Expiration - TimeSpan.FromMinutes(30)
                })
                .ToList();
        }

        public async Task<string> RemoveDeviceAsync(Guid userId, string userAgent, string platform, string ipAddress, string requestToken)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", userId);
                    return "User not found.";
                }

                var loginToken = user.LoginTokens?.FirstOrDefault(t => t.DeviceInfo.UserAgent == userAgent && t.DeviceInfo.Platform == platform && t.IpAddress == ipAddress && t.IsActive);
                if (loginToken == null)
                {
                    _logger.LogWarning("Device not found or inactive for user {UserId}.", userId);
                    return "Device not found or inactive.";
                }

                if (loginToken.Token == requestToken)
                {
                    _logger.LogWarning("Attempt to deactivate current device for user {UserId}.", userId);
                    return "The current device cannot be deactivated.";
                }

                loginToken.IsActive = false;
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("Device removed successfully for user: {UserId}, UserAgent: {UserAgent}, Platform: {Platform}", userId, userAgent, platform);
                return "Device removed successfully.";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing device for user: {UserId}.", userId);
                return "An unexpected error occured.";
            }
        }
    }
}
