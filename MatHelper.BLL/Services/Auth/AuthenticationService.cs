using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Enums;
using MatHelper.DAL.Models;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace MatHelper.BLL.Services
{
    public class AuthenticationService: IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAppTwoFactorSessionRepository _twoFactorSessionRepository;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IEmailConfirmationRepository _emailConfirmationRepository;
        private readonly IEmailLoginCodeRepository _emailLoginCodeRepository;
        private readonly IAuthLogRepository _authLogRepository;
        private readonly IMailService _mailService;
        private readonly ISecurityService _securityService;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly IRegistrationService _registrationService;
        private readonly ISecurityPolicyService _securityPolicy;
        private readonly IEmailAuthService _emailAuthService;
        private readonly ILoginService _loginService;
        private readonly IRecoveryService _recoveryService;
        private readonly ITwoFactorAuthService _twoFactorAuthService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(IUserRepository userRepository, IAppTwoFactorSessionRepository appTwoFactorSessionRepository, ITwoFactorService twoFactorService, IEmailConfirmationRepository emailConfirmationRepository, IEmailLoginCodeRepository emailLoginCodeRepository, IAuthLogRepository authLogRepository, IMailService mailService, ISecurityService securityService, ILoginAttemptService loginAttemptService, IRegistrationService registrationService, ISecurityPolicyService securityPolicy, IEmailAuthService emailAuthService, ILoginService loginService, IRecoveryService recoveryService, ITwoFactorAuthService twoFactorAuthService, ITokenService tokenService, ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _twoFactorSessionRepository = appTwoFactorSessionRepository;
            _twoFactorService = twoFactorService;
            _emailConfirmationRepository = emailConfirmationRepository;
            _emailLoginCodeRepository = emailLoginCodeRepository;
            _authLogRepository = authLogRepository;
            _mailService = mailService;
            _securityService = securityService;
            _loginAttemptService = loginAttemptService;
            _registrationService = registrationService;
            _securityPolicy = securityPolicy;
            _emailAuthService = emailAuthService;
            _loginService = loginService;
            _recoveryService = recoveryService;
            _twoFactorAuthService = twoFactorAuthService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<bool> RegisterUserAsync(UserDto userDto, DeviceInfo deviceInfo, string ipAddress)
        {
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

            await _securityPolicy.EnforceRegistrationIpLimitAsync(ipAddress);

            await _registrationService.EnsureEmailAndUsernameUniqueAsync(userDto.Email, userDto.UserName);

            var passwordHash = _securityService.HashPassword(userDto.Password);

            var user = await _registrationService.BuildNewUserAsync(userDto, passwordHash);

            await _registrationService.CreateInactiveInitialSessionAsync(user, deviceInfo, ipAddress);

            await _userRepository.AddUserAsync(user);
            
            var emailToken = await _emailAuthService.CreateEmailConfirmationTokenAsync(user);

            await _emailConfirmationRepository.AddEmailConfirmationTokenAsync(emailToken);

            await _userRepository.SaveChangesAsync();
            await _emailConfirmationRepository.SaveChangesAsync();

            await _mailService.SendConfirmationEmailAsync(user.Email, emailToken.Token);

            return true;
        }

        public async Task<ConfirmTokenResult> ConfirmEmailAsync(string token)
        {
            return await _emailAuthService.ConfirmEmailAsync(token);
        }

        public async Task<LoginResponse> LoginUserAsync(LoginDto loginDto, DeviceInfo deviceInfo, string ipAddress)
        {
            try
            {
                await _loginAttemptService.CheckIpBlockedAsync(ipAddress);

                var user = await _loginService.ValidateUserCredentialsAsync(loginDto.Email, loginDto.Password);

                await _loginService.EnsureUserCanLoginAsync(user);

                await _loginAttemptService.ResetAttemptsAsync(ipAddress);

                _securityPolicy.ValidateDeviceInfo(deviceInfo);

                await _loginService.CleanupUserSessionsAsync(user, deviceInfo, ipAddress);

                await _loginService.ApplySessionLimitsAsync(user);

                var twoFactor = await _twoFactorService.GetTwoFactorAsync(user.Id, "totp");

                if (twoFactor != null && twoFactor.IsEnabled && twoFactor.AlwaysAsk)
                {
                    var session = await _twoFactorAuthService.CreateTwoFactorSessionAsync(user, deviceInfo, ipAddress, loginDto.Remember);

                    return new LoginResponse
                    {
                        Message = "Enter the 2FA code from your app",
                        SessionKey = session.SessionKey
                    };
                }

                var lastToken = user.LoginTokens?
                    .Where(t => t.IpAddress == ipAddress)
                    .OrderByDescending(t => t.Expiration)
                    .FirstOrDefault();

                var unfamiliar = _securityPolicy.IsUnfamiliar(lastToken, ipAddress, deviceInfo.UserAgent!); 

                if (unfamiliar)
                {
                    var emailCode = await _emailAuthService.CreateEmailLoginCodeAsync(user, deviceInfo, ipAddress, loginDto.Remember);

                    return new LoginResponse
                    {
                        Message = "Check your email for the code",
                        SessionKey = emailCode.SessionKey
                    };
                }

                var token = await _loginService.IssueLoginTokenAsync(user, deviceInfo, ipAddress, loginDto.Remember);

                user.LoginTokens!.Add(token);
                await _userRepository.SaveChangesAsync();

                if (await _securityService.CheckSuspiciousActivityAsync(ipAddress, deviceInfo.UserAgent!, deviceInfo.Platform!))
                    throw new UnauthorizedAccessException("Suspicious activity detected. Accounts blocked.");

                return new LoginResponse
                {
                    AccessToken = token.Token,
                    RefreshToken = token.RefreshToken,
                    RefreshTokenExpiration = token.RefreshTokenExpiration
                };
            }
            catch (Exception ex) when (
                ex is UnauthorizedAccessException ||
                ex is InvalidOperationException ||
                ex is InvalidDataException)
            {
                await _loginAttemptService.RegisterFailedAttemptAsync(ipAddress);
                await _authLogRepository.LogAuthAsync("Unknown", ipAddress, deviceInfo.UserAgent ?? "Unknown", "Failed", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                await _authLogRepository.LogAuthAsync("Unknown", ipAddress, deviceInfo.UserAgent ?? "Unknown", "Failed", "Unknown error occurred.");
                throw new Exception("Unknown error occurred during request", ex);
            }
        }

        public async Task<LoginResponse> ConfirmEmailCodeAsync(string code, string sessionKey)
        {
            var emailCode = await _emailLoginCodeRepository.GetBySessionKeyAsync(sessionKey);
            if (emailCode == null)
                throw new UnauthorizedAccessException("Invalid or expired session key.");

            if (emailCode.Code != code)
                throw new UnauthorizedAccessException("Invalid confirmation code.");

            var user = await _userRepository.GetUserByIdAsync(emailCode.UserId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (user.IsBlocked)
                throw new UnauthorizedAccessException("User is banned.");

            emailCode.IsUsed = true;
            await _emailLoginCodeRepository.SaveChangesAsync();

            var deviceInfo = new DeviceInfo
            {
                UserAgent = emailCode.UserAgent,
                Platform = emailCode.Platform
            };

            var loginToken = await _tokenService.IssueLoginTokenAsync(
                user,
                deviceInfo,
                emailCode.IpAddress,
                emailCode.Remember
            );

            user.LoginTokens!.Add(loginToken);
            await _userRepository.SaveChangesAsync();

            await _authLogRepository.LogAuthAsync(
                user.Id.ToString(),
                emailCode.IpAddress,
                deviceInfo.UserAgent,
                "Success via email confirmation"
            );

            return new LoginResponse
            {
                AccessToken = loginToken.Token,
                RefreshToken = loginToken.RefreshToken
            };
        }

        public async Task<LoginResponse> ConfirmTwoFactorCodeAsync(string code, string sessionKey)
        {
            var result = await _twoFactorAuthService.ValidateTwoFactorSessionAsync(sessionKey, code);

            if (!result.Success)
                throw new UnauthorizedAccessException(result.Error ?? "2FA validation failed.");

            if (!result.UserId.HasValue)
                throw new UnauthorizedAccessException("UserId is missing.");

            var user = await _userRepository.GetUserByIdAsync(result.UserId.Value);

            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var session = await _twoFactorSessionRepository.GetBySessionKeyAsync(sessionKey);

            if (session == null)
                throw new UnauthorizedAccessException("Session not found.");

            var deviceInfo = new DeviceInfo
            {
                UserAgent = session.UserAgent,
                Platform = session.Platform
            };

            var token = await _loginService.IssueLoginTokenAsync(
                user,
                deviceInfo,
                session.IpAddress,
                session.Remember
            );

            user.LoginTokens ??= new List<LoginToken>();
            user.LoginTokens.Add(token);
            
            session.IsUsed = true;

            await _userRepository.SaveChangesAsync();
            await _twoFactorSessionRepository.SaveChangesAsync();

            await _authLogRepository.LogAuthAsync(
                user.Id.ToString(),
                session.IpAddress,
                deviceInfo.UserAgent,
                "Success via 2FA"
            );

            return new LoginResponse
            {
                AccessToken = token.Token,
                RefreshToken = token.RefreshToken
            };
        }

        public async Task<bool> SendRecoverPasswordLinkAsync(string email)
        {
            try
            {
                return await _recoveryService.SendRecoveryEmailAsync(email);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error while sending password recovery link.");
                throw;
            }
        }

        public async Task<RecoverPasswordResult> RecoverPassword(string token, string password)
        {
            try
            {
                return await _recoveryService.ResetPasswordAsync(token, password);
            }
            catch (Exception ex) 
            {
                _logger.LogError("Unknown error occurred during request: {ex}", ex);

                return RecoverPasswordResult.Failed;
            }
        }
    }
}