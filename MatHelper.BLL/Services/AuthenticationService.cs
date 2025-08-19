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
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MatHelper.BLL.Services
{
    public class AuthenticationService: IAuthenticationService
    {
        private readonly UserRepository _userRepository;
        private readonly AppTwoFactorSessionRepository _twoFactorSessionRepository;
        private readonly ITokenGeneratorService _tokenGeneratorService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly EmailConfirmationRepository _emailConfirmationRepository;
        private readonly EmailLoginCodeRepository _emailLoginCodeRepository;
        private readonly PasswordRecoveryRepository _passwordRecoveryRepository;
        private readonly AuthLogRepository _authLogRepository;
        private readonly IMailService _mailService;
        private readonly JwtOptions _jwtOptions;
        private readonly ISecurityService _securityService;
        private readonly ITokenService _tokenService;
        private readonly ILogger _logger;

        public AuthenticationService(UserRepository userRepository, AppTwoFactorSessionRepository appTwoFactorSessionRepository, ITokenGeneratorService tokenGeneratorService, ITwoFactorService twoFactorService, EmailConfirmationRepository emailConfirmationRepository, EmailLoginCodeRepository emailLoginCodeRepository, PasswordRecoveryRepository passwordRecoveryRepository, AuthLogRepository authLogRepository, IMailService mailService, JwtOptions jwtOptions, ISecurityService securityService, ITokenService tokenService, ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _twoFactorSessionRepository = appTwoFactorSessionRepository;
            _tokenGeneratorService = tokenGeneratorService;
            _twoFactorService = twoFactorService;
            _emailConfirmationRepository = emailConfirmationRepository;
            _emailLoginCodeRepository = emailLoginCodeRepository;
            _passwordRecoveryRepository = passwordRecoveryRepository;
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
                if (!existingUserByEmail.IsActive)
                {
                    var existingToken = await _emailConfirmationRepository.GetTokenByUserIdAsync(existingUserByEmail.Id);
                    if (existingToken != null && existingToken.ExpirationDate > DateTime.UtcNow) 
                    {
                        _logger.LogWarning("User with email {Email} has not confirmed email yet.", existingUserByEmail.Email);
                        throw new InvalidOperationException("The account awaits confirmation. Follow the link in the email.");
                    }
                    else
                    {
                        _logger.LogInformation("Email confirmation token expired or null. Deleting user {Email}", existingUserByEmail.Email);
                        await _userRepository.DeleteUserAsync(existingUserByEmail);
                        _userRepository.Detach(existingUserByEmail);
                    }
                }
                else
                {
                    _logger.LogError("Email is already used by another user: {Email}", userDto.Email);
                    throw new InvalidOperationException("Email is already used by another user.");
                }
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

            var accessToken = _tokenGeneratorService.GenerateJwtToken(user, deviceInfo);
            var refreshToken = _tokenGeneratorService.GenerateRefreshToken();

            /* Temporary inactive token used to track registration attempt (not for authentication yet) */
            var loginToken = new LoginToken
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow,
                RefreshTokenExpiration = DateTime.UtcNow,
                UserId = user.Id,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
                IsActive = false
            };

            user.LoginTokens = new List<LoginToken> { loginToken };

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

            await _emailConfirmationRepository.AddEmailConfirmationTokenAsync(emailConfirmationToken);

            await _userRepository.SaveChangesAsync();
            await _emailConfirmationRepository.SaveChangesAsync();

            _logger.LogInformation("Activation token generated and stored for user {UserName}", userDto.UserName);

            _logger.LogInformation("Sending email confirmation to {Email} with token link.", userDto.Email);
            await _mailService.SendConfirmationEmailAsync(user.Email, activationToken);

            return true;
        }

        public async Task<ConfirmTokenResult> ConfirmEmailAsync(string token)
        {
            try
            {
                var (result, user) = await _emailConfirmationRepository.ConfirmUserByTokenAsync(token);
                
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

                    await _emailConfirmationRepository.AddEmailConfirmationTokenAsync(newEmailToken);
                    await _emailConfirmationRepository.SaveChangesAsync();
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
            if (user == null) throw new InvalidOperationException("User not found.");

            if (!user.IsActive) throw new UnauthorizedAccessException("Please activate your account by following the link sent to your email.");

            try
            {
                if (user.IsBlocked) throw new UnauthorizedAccessException("User is banned.");

                if (!_securityService.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt)) throw new UnauthorizedAccessException("Invalid password.");

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

                var activeTwoFactor = await _twoFactorService.GetTwoFactorAsync(user.Id, "totp");
                if (activeTwoFactor != null && activeTwoFactor.IsEnabled && activeTwoFactor.AlwaysAsk) 
                {
                    var sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                    var appTwoFactorSession = new AppTwoFactorSession
                    {
                        UserId = user.Id,
                        SessionKey = sessionKey,
                        Expiration = DateTime.UtcNow.AddMinutes(10),
                        IpAddress = ipAddress,
                        UserAgent = deviceInfo.UserAgent,
                        Platform = deviceInfo.Platform,
                        Remember = loginDto.Remember,
                        IsUsed = false
                    };

                    await _twoFactorSessionRepository.AddSessionAsync(appTwoFactorSession);

                    return new LoginResponse
                    {
                        Message = "Enter the 2FA code from your app",
                        SessionKey = sessionKey
                    };
                }

                var lastTokenForIp = user.LoginTokens?
                    .Where(t => t.IpAddress == ipAddress)
                    .OrderByDescending(t => t.Expiration)
                    .FirstOrDefault();

                bool isUnfamiliarLocation = lastTokenForIp == null
                    || (DateTime.UtcNow - lastTokenForIp.Expiration).TotalDays > 14;

                if (isUnfamiliarLocation)
                {
                    int codeInt;
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        byte[] bytes = new byte[4];
                        rng.GetBytes(bytes);
                        codeInt = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
                        codeInt = 100000 + (codeInt % 900000);
                    }

                    var code = codeInt.ToString();

                    var sessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

                    var emailCode = new EmailLoginCode
                    {
                        UserId = user.Id,
                        Code = code,
                        SessionKey = sessionKey,
                        IpAddress = ipAddress,
                        UserAgent = deviceInfo.UserAgent,
                        Platform = deviceInfo.Platform,
                        Remember = loginDto.Remember,
                        Expiration = DateTime.UtcNow.AddMinutes(15),
                        IsUsed = false
                    };

                    await _emailLoginCodeRepository.AddCodeAsync(emailCode);

                    await _mailService.SendIpConfirmationCodeEmailAsync(user.Email, code);

                    await _authLogRepository.LogAuthAsync(user.Id.ToString(), ipAddress, deviceInfo.UserAgent, "Sent confirmation code");

                    return new LoginResponse
                    {
                        Message = "Check your email for the code",
                        SessionKey = sessionKey
                    };
                }

                var refreshTokenExpiration = loginDto.Remember == true ? DateTime.UtcNow.AddDays(28) : DateTime.UtcNow.AddHours(6);

                var accessToken = _tokenGeneratorService.GenerateJwtToken(user, deviceInfo);
                var refreshToken = _tokenGeneratorService.GenerateRefreshToken();

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

        public async Task<LoginResponse> ConfirmEmailCodeAsync(string code, string sessionKey)
        {
            var emailCode = await _emailLoginCodeRepository.GetBySessionKeyAsync(sessionKey);
            if (emailCode == null) throw new UnauthorizedAccessException("Invalid or expired session key.");

            if (emailCode.Code != code) throw new UnauthorizedAccessException("Invalid confirmation code.");

            var user = await _userRepository.GetUserByIdAsync(emailCode.UserId);
            if (user == null) throw new UnauthorizedAccessException("User not found.");
            if (user.IsBlocked) throw new UnauthorizedAccessException("User is banned.");

            emailCode.IsUsed = true;
            await _emailLoginCodeRepository.SaveChangesAsync();

            var deviceInfo = new DeviceInfo
            {
                UserAgent = emailCode.UserAgent,
                Platform = emailCode.Platform
            };
            var ipAddress = emailCode.IpAddress;

            var accessToken = _tokenGeneratorService.GenerateJwtToken(user, deviceInfo);
            var refreshToken = _tokenGeneratorService.GenerateRefreshToken();

            var refreshTokenExpiration = emailCode.Remember ? DateTime.UtcNow.AddDays(28) : DateTime.UtcNow.AddHours(6);

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

            await _authLogRepository.LogAuthAsync(user.Id.ToString(), ipAddress, deviceInfo.UserAgent, "Success via email confirmation");

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<LoginResponse> ConfirmTwoFactorCodeAsync(string code, string sessionKey)
        {
            var sessions = await _twoFactorSessionRepository.GetAllSessionKeysAsync();
            foreach(var s in sessions)
            {
                _logger.LogInformation($"{s} - {sessionKey}");
            }
            
            var session = await _twoFactorSessionRepository.GetBySessionKeyAsync(sessionKey);
            if (session == null) throw new UnauthorizedAccessException("Invalid or expired session key.");

            var user = await _userRepository.GetUserByIdAsync(session.UserId);
            if (user == null) throw new UnauthorizedAccessException("User not found.");
            if (user.IsBlocked) throw new UnauthorizedAccessException("User is banned.");

            var twoFactor = await _twoFactorService.GetTwoFactorAsync(user.Id, "totp");
            if (twoFactor == null || !twoFactor.IsEnabled) throw new UnauthorizedAccessException("Two-factor authentication is not enabled.");

            if (twoFactor.Secret == null) throw new UnauthorizedAccessException("Invalid key");
            var isValid = _twoFactorService.VerifyTotp(twoFactor.Secret, code);
            if (!isValid) throw new UnauthorizedAccessException("Invalid 2FA code.");

            session.IsUsed = true;
            await _twoFactorSessionRepository.SaveChangesAsync();

            var deviceInfo = new DeviceInfo
            {
                UserAgent = session.UserAgent,
                Platform = session.Platform
            };
            var ipAddress = session.IpAddress;

            var accessToken = _tokenGeneratorService.GenerateJwtToken(user, deviceInfo);
            var refreshToken = _tokenGeneratorService.GenerateRefreshToken();

            var refreshTokenExpiration = session.Remember ? DateTime.UtcNow.AddDays(28) : DateTime.UtcNow.AddHours(6);
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

            await _authLogRepository.LogAuthAsync(user.Id.ToString(), ipAddress, deviceInfo.UserAgent, "Success via 2FA");

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };
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

            await _passwordRecoveryRepository.AddPasswordRecoveryTokenAsync(recoveryToken);
            await _passwordRecoveryRepository.SaveChangesAsync();

            _logger.LogInformation("Password recovery token created for user: {Email}", email);
            await _mailService.SendPasswordRecoveryEmailAsync(user.Email, recoveryToken.Token);

            return true;
        }

        public async Task<RecoverPasswordResult> RecoverPassword(string token, string password)
        {
            try
            {
                //var sw = Stopwatch.StartNew();
                var (result, user) = await _passwordRecoveryRepository.GetUserByRecoveryToken(token);
                //_logger.LogInformation("GetUserByRecoveryToken finished in {Time}ms with result: {Result}", sw.ElapsedMilliseconds, result);

                if (user == null)
                {
                    return result;
                }

                if (result == RecoverPasswordResult.Success)
                {
                    //_logger.LogInformation("Changing password...");
                    var salt = _securityService.GenerateSalt();
                    var hashedPassword = _securityService.HashPassword(password, salt);

                    var changePasswordResult = await _userRepository.ChangePassword(user, hashedPassword, salt);
                    //_logger.LogInformation("Password change result: {Result}", changePasswordResult);
                }

                return result;
            }
            catch (Exception ex) 
            {
                _logger.LogError("Unknown error occurred during request: {ex}", ex);
                return RecoverPasswordResult.Failed;
            }
        }
    }
}