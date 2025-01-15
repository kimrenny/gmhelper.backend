using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MatHelper.BLL.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserRepository _userRepository;
        private readonly JwtOptions _jwtOptions;
        private readonly ISecurityService _securityService;
        private readonly ILogger _logger;

        public UserManagementService(UserRepository userRepository, JwtOptions jwtOptions, ISecurityService securityService, ILogger<UserManagementService> logger)
        {
            _userRepository = userRepository;
            _jwtOptions = jwtOptions;
            _securityService = securityService;
            _logger = logger;
        }

        public async Task<User> GetUserDetailsAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new InvalidDataException("User not found.");

            return user;
        }

        public async Task<byte[]> GetUserAvatarAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            return user!.Avatar;
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

        public async Task SaveUserAvatarAsync(string userId, byte[] avatarBytes)
        {
            var user = await _userRepository.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null) throw new Exception("User not found.");

            user.Avatar = avatarBytes;
            await _userRepository.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            _logger.LogInformation("Attempting to update user with ID {UserId}. CurrentPassword: {CurrentPassword}", userId, request.CurrentPassword);

            if (string.IsNullOrEmpty(request.CurrentPassword) || !this._securityService.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Invalid current password for user {UserId}.", userId);
                throw new InvalidOperationException("Invalid current password.");
            }

            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Email is already used by another user");
                    throw new InvalidOperationException("Email is already used by another user.");
                }
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.Nickname) && request.Nickname != user.Username)
            {
                var existingUser = await _userRepository.GetUserByUsernameAsync(request.Nickname);
                if(existingUser != null)
                {
                    _logger.LogWarning("Username is already used by another user");
                    throw new InvalidOperationException("Username is already used by another user.");
                }

                user.Username = request.Nickname;
            }

            if (!string.IsNullOrEmpty(request.NewPassword))
            {

                if(request.NewPassword.Length < 8)
                {
                    throw new InvalidOperationException("New password is too short.");
                }

                user.PasswordHash = this._securityService.HashPassword(request.NewPassword, user.PasswordSalt);
            }

            await _userRepository.UpdateUserAsync(user);
        }

        public async Task<string> RemoveDeviceAsync(Guid userId, string userAgent, string platform, string ipAddress)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if(user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", userId);
                    return "User not found.";
                }

                var loginToken = user.LoginTokens?.FirstOrDefault(t => t.DeviceInfo.UserAgent == userAgent && t.DeviceInfo.Platform == platform && t.IpAddress == ipAddress && t.IsActive);
                if(loginToken == null)
                {
                    _logger.LogWarning("Device not found or inactive for user {UserId}.", userId);
                    return "Device not found or inactive.";
                }

                loginToken.IsActive = false;
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation("Device removed successfully for user: {UserId}, UserAgent: {UserAgent}, Platform: {Platform}", userId, userAgent, platform);
                return "Device removed successfully.";
                
            } 
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error removing device for user: {UserId}.", userId);
                return "An unexpected error occured.";
            }
        }
    }
}