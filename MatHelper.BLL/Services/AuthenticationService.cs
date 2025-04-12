using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
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
        private readonly IMailService _mailService;
        private readonly JwtOptions _jwtOptions;
        private readonly ISecurityService _securityService;
        private readonly ITokenService _tokenService;
        private readonly ILogger _logger;

        public AuthenticationService(UserRepository userRepository, AuthLogRepository authLogRepository, IMailService mailService, JwtOptions jwtOptions, ISecurityService securityService, ITokenService tokenService, ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _authLogRepository = authLogRepository;
            _mailService = mailService;
            _jwtOptions = jwtOptions;
            _securityService = securityService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<bool> RegisterUserAsync(UserDto userDto, DeviceInfo deviceInfo, string ipAddress)
        {
            _logger.LogInformation("Attempting to register user with email: {Email} and username: {Username}", userDto.Email, userDto.UserName);

            if (string.IsNullOrWhiteSpace(userDto.Email))
            {
                _logger.LogError("Email cannot be null or empty.");
                throw new ArgumentException("Email cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(userDto.UserName))
            {
                _logger.LogError("Username cannot be null or empty.");
                throw new ArgumentException("Username cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                _logger.LogError("IP Address cannot be null or empty.");
                throw new ArgumentException("IP Address cannot be null or empty.");
            }

            var existingUserByEmail = await _userRepository.GetUserByEmailAsync(userDto.Email);
            if (existingUserByEmail != null)
            {
                _logger.LogError("Email is already used by another user: {Email}", userDto.Email);
                throw new InvalidOperationException("Email is already used by another user.");
            }

            var existingUserByUsername = await _userRepository.GetUserByUsernameAsync(userDto.UserName);
            if (existingUserByUsername != null)
            {
                _logger.LogError("Username is already used by another user: {Username}", userDto.UserName);
                throw new InvalidOperationException("Username is already used by another user.");
            }

            var userCountByIp = await _userRepository.GetUserCountByIpAsync(ipAddress);

            if (userCountByIp >= 3)
            {
                _logger.LogWarning("IP address {IpAddress} has exceeded the registration limit.", ipAddress);
                var usersToBlock = await _userRepository.GetUsersByIpAsync(ipAddress);

                if (usersToBlock == null || !usersToBlock.Any())
                    throw new InvalidOperationException("No users found with the specified IP address.");

                foreach (var blockedUser in usersToBlock)
                {
                    if (blockedUser != null)
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
                _logger.LogWarning("Accounts from IP {IpAddress} have been blocked due to violation of service rules.", ipAddress);
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
                IsActive = false,
            };

            await _userRepository.AddUserAsync(user);

            _logger.LogInformation("User {UserName} created successfully. Awaiting email confirmation.", userDto.UserName);

            var activationToken = Guid.NewGuid().ToString();
            var emailConfirmationToken = new EmailConfirmationToken
            {
                Token = activationToken,
                UserId = user.Id,
                ExpirationDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false,
                User = user,
            };

            await _userRepository.AddEmailConfirmationTokenAsync(emailConfirmationToken);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Activation token generated and stored for user {UserName}", userDto.UserName);

            _logger.LogInformation("Sending email confirmation to {Email} with token link.", userDto.Email);
            await _mailService.SendConfirmationEmailAsync(user.Email, activationToken);

            return true;
        }

        public async Task<ConfirmTokenResult> ConfirmEmailAsync(string token)
        {
            try
            {
                var (result, user) = await _userRepository.ConfirmUserByTokenAsync(token);
                
                if(result == ConfirmTokenResult.TokenExpired && user is not null)
                {
                    var newToken = Guid.NewGuid().ToString();
                    var newEmailToken = new EmailConfirmationToken
                    {
                        Token = newToken,
                        UserId = user.Id,
                        ExpirationDate = DateTime.UtcNow.AddHours(1),
                        IsUsed = false,
                        User = user,
                    };

                    await _userRepository.AddEmailConfirmationTokenAsync(newEmailToken);
                    await _userRepository.SaveChangesAsync();
                    await _mailService.SendConfirmationEmailAsync(user.Email, newToken);
                }

                return result;
            }
            catch (InvalidDataException)
            {
                throw new InvalidDataException("Invalid or expired token");
            }
            catch (Exception ex)
            {
                throw new Exception("Unknown error occurred during request", ex);
            }
        }

        public async Task<LoginResponse> LoginUserAsync(LoginDto loginDto, DeviceInfo deviceInfo, string ipAddress)
        {
            var user = await _userRepository.GetUserByEmailAsync(loginDto.Email);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Please activate your account by following the link sent to your email.");
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

                return new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                };
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


        public async Task<bool> SendRecoverPasswordLinkAsync(string email)
        {
            _logger.LogInformation("Attempting password recovery for email: {Email}", email);

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogError("Email cannot be null or empty.");
                throw new ArgumentException("Email cannot be null or empty.");
            }

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Password recovery attempted for non-existent email: {Email}", email);
                return false;
            }

            var recoveryToken = new PasswordRecoveryToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = user.Id,
                ExpirationDate = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                User = user
            };

            await _userRepository.AddPasswordRecoveryTokenAsync(recoveryToken);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Password recovery token created for user: {Email}", email);
            await _mailService.SendPasswordRecoveryEmailAsync(user.Email, recoveryToken.Token);

            return true;
        }
    }
}