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
    public class AuthenticationService: IAuthenticationService
    {
        private readonly UserRepository _userRepository;
        private readonly AuthLogRepository _authLogRepository;
        private readonly JwtOptions _jwtOptions;
        private readonly ISecurityService _securityService;
        private readonly ITokenService _tokenService;
        private readonly ILogger _logger;

        public AuthenticationService(UserRepository userRepository, AuthLogRepository authLogRepository, JwtOptions jwtOptions, ISecurityService securityService, ITokenService tokenService, ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _authLogRepository = authLogRepository;
            _jwtOptions = jwtOptions;
            _securityService = securityService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<bool> RegisterUserAsync(UserDto userDto, DeviceInfo deviceInfo, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(userDto.Email))
                throw new ArgumentException("Email cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(userDto.UserName))
                throw new ArgumentException("Username cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP Address cannot be null or empty.");

            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(userDto.Email);
            if (existingUserByEmail != null) throw new InvalidOperationException("Email is already used by another user.");

            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(userDto.UserName);
            if (existingUserByUsername != null) throw new InvalidOperationException("Username is already used by another user.");

            var userCountByIp = await _userRepository.GetUserCountByIpAsync(ipAddress);

            if (userCountByIp >= 3)
            {
                var usersToBlock = await _userRepository.GetUsersByIpAsync(ipAddress);

                if (usersToBlock == null || !usersToBlock.Any())
                    throw new InvalidOperationException("No users found with the specified IP address.");

                if(usersToBlock != null && usersToBlock.Count > 0)
                {
                    foreach (var blockedUser in usersToBlock)
                    {
                        if (blockedUser != null)
                        {
                            blockedUser.IsBlocked = true;
                            if(blockedUser.LoginTokens != null)
                            {
                                IEnumerable<LoginToken> tokens = blockedUser.LoginTokens.Where(t => t.IsActive);
                                if (tokens.Count() > 0)
                                {
                                    foreach (var token in tokens)
                                    {
                                        if (token != null)
                                            token.IsActive = false;
                                    }
                                }
                            }
                        }
                    }
                }

                await _userRepository.SaveChangesAsync();
                throw new UnauthorizedAccessException("Violation of service rules. All user accounts have been blocked.");
            }

            var salt = _securityService.GenerateSalt();
            var hashedPassword = _securityService.HashPassword(userDto.Password, salt);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = userDto.UserName,
                Email = userDto.Email,
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                Avatar = null,
                Role = "User",
            };

            await _userRepository.AddUserAsync(user);

            var loginToken = new LoginToken
            {
                Token = _tokenService.GenerateJwtToken(user, deviceInfo),
                RefreshToken = _tokenService.GenerateRefreshToken(),
                Expiration = DateTime.UtcNow.AddMinutes(30),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                IsActive = true
            };

            var createdUser = await _userRepository.GetUserByEmailAsync(userDto.Email);

            if (createdUser != null)
            {
                createdUser.LoginTokens ??= new List<LoginToken>();
                createdUser.LoginTokens.Add(loginToken);
                await _userRepository.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException("Error due add token to the user.");
            }

            if (deviceInfo.UserAgent == null || deviceInfo.Platform == null)
            {
                _logger.LogError("deviceInfo does not meet the requirements");
                throw new InvalidDataException("deviceInfo does not meet the requirements");
            }

            if (await _securityService.CheckSuspiciousActivityAsync(ipAddress, deviceInfo.UserAgent, deviceInfo.Platform))
            {
                throw new UnauthorizedAccessException("Suspicious activity detected. Accounts blocked.");
            }

            return true;
        }

        public async Task<(string AccessToken, string RefreshToken)> LoginUserAsync(LoginDto loginDto, DeviceInfo deviceInfo, string ipAddress)
        {
            var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            try
            {
                if (user.IsBlocked)
                {
                    throw new UnauthorizedAccessException("User is blocked");
                }

                if (!_securityService.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
                {
                    throw new UnauthorizedAccessException("Invalid password.");
                }

                if (deviceInfo.UserAgent == null || deviceInfo.Platform == null)
                {
                    _logger.LogError("deviceInfo does not meet the requirements");
                    throw new InvalidDataException("deviceInfo does not meet the requirements");
                }

                var expiredTokens = user.LoginTokens!.Where(t => t.Expiration <= DateTime.UtcNow).ToList();
                foreach (var expiredToken in expiredTokens)
                {
                    expiredToken.IsActive = false;
                    //user.LoginTokens!.Remove(expiredToken);
                }
                await _userRepository.SaveChangesAsync();

                var activeTokens = user.LoginTokens!.Where(t => t.DeviceInfo.UserAgent == deviceInfo.UserAgent && t.DeviceInfo.Platform == deviceInfo.Platform && t.IpAddress == ipAddress && t.IsActive).ToList();

                if (activeTokens.Count >= 3)
                {
                    foreach (var activeToken in activeTokens)
                    {
                        activeToken.IsActive = false;
                    }
                }

                var activeTokenCount = user.LoginTokens!.Count(t => t.IsActive);
                if (activeTokenCount >= 5)
                {
                    var oldestToken = user.LoginTokens!.Where(t => t.IsActive).MinBy(t => t.Expiration);

                    if (oldestToken != null)
                    {
                        oldestToken.IsActive = false;
                    }
                }

                var refreshTokenExpiration = loginDto.Remember == true ? DateTime.UtcNow.AddDays(28) : DateTime.UtcNow.AddHours(6);

                var accessToken = _tokenService.GenerateJwtToken(user, deviceInfo);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var loginToken = new LoginToken
                {
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(30),
                    RefreshTokenExpiration = refreshTokenExpiration,
                    UserId = user.Id,
                    DeviceInfo = deviceInfo,
                    IpAddress = ipAddress,
                    IsActive = true
                };

                user.LoginTokens!.Add(loginToken);
                await _userRepository.SaveChangesAsync();

                await _authLogRepository.LogAuthAsync(user.Id.ToString(), ipAddress, deviceInfo.UserAgent, "Success");

                if (await _securityService.CheckSuspiciousActivityAsync(ipAddress, deviceInfo.UserAgent, deviceInfo.Platform))
                {
                    throw new UnauthorizedAccessException("Suspicious activity detected. Accounts blocked.");
                }

                return (AccessToken: accessToken, RefreshToken: refreshToken);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException
                        || ex is InvalidOperationException
                        || ex is InvalidDataException)
            {
                await _authLogRepository.LogAuthAsync(
                    user?.Id.ToString() ?? "Unknown",
                    ipAddress,
                    deviceInfo.UserAgent ?? "Unknown",
                    "Failed",
                    ex.Message
                );
                throw;
            }
            catch (Exception ex)
            {
                await _authLogRepository.LogAuthAsync(
                    user?.Id.ToString() ?? "Unknown",
                    ipAddress,
                    deviceInfo.UserAgent ?? "Unknown",
                    "Failed",
                    "Unknown error occurred."
                );
                throw new Exception("Unknown error occurred during request", ex);
            }
        }


        public async Task<bool> RecoverPasswordAsync(PasswordRecoveryDto recoveryDto)
        {
            var user = await _userRepository.GetUserByEmailAsync(recoveryDto.Email);
            if (user == null) return false;

            return true;
        }
    }
}