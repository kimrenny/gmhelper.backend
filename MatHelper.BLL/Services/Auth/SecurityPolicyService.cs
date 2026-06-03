using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace MatHelper.BLL.Services
{
    public class SecurityPolicyService : ISecurityPolicyService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SecurityPolicyService> _logger;

        private const byte MaxUsersPerIp = 3;
        private const int UnfamiliarLocationThresholdDays = 14;

        public SecurityPolicyService(IUserRepository userRepository, ILogger<SecurityPolicyService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task EnforceRegistrationIpLimitAsync(string ip)
        {
            var userCountByIp = await _userRepository.GetUserCountByIpAsync(ip);

            if (userCountByIp < MaxUsersPerIp)
                return;

            var usersToBlock = await _userRepository.GetUsersByIpAsync(ip);

            if (usersToBlock == null || !usersToBlock.Any())
                throw new InvalidOperationException("No users found with the specified IP address.");

            _logger.LogWarning("IP address {IpAddress} has exceeded the registration limit.", ip);

            foreach (var blockedUser in usersToBlock)
            {
                if (blockedUser != null && blockedUser.Role != "Owner")
                {
                    blockedUser.IsBlocked = true;

                    if (blockedUser.LoginTokens != null)
                    {
                        var tokens = blockedUser.LoginTokens.Where(t => t.IsActive);
                        foreach (var token in tokens)
                        {
                            if (token != null)
                                token.IsActive = false;
                        }
                    }
                }
            }

            await _userRepository.SaveChangesAsync();

            _logger.LogWarning("Accounts from IP {IpAddress} have been blocked due to violation of service rules.", ip);
            throw new UnauthorizedAccessException("Violation of service rules. All user accounts have been blocked.");
        }

        public void ValidateDeviceInfo(DeviceInfo deviceInfo)
        {
            if (deviceInfo.UserAgent == null || deviceInfo.Platform == null)
                throw new InvalidDataException("deviceInfo does not meet the requirements");
        }

        public bool IsUnfamiliar(LoginToken? lastToken, string ipAddress, string userAgent)
        {
            if (lastToken == null)
                return true;

            if (lastToken.IpAddress != ipAddress)
                return true;

            if (lastToken.DeviceInfo?.UserAgent != userAgent)
                return true;

            return false;
        }
    }
}