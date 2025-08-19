using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.OpenApi.Validations;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using MatHelper.CORE.Enums;
using MatHelper.API.Common;
using System.Diagnostics;

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ITokenService _tokenService;
        private readonly IUserManagementService _userManagementService;
        private readonly IDeviceManagementService _deviceManagementService;
        private readonly IMailService _mailService;
        private readonly ILogger<UserController> _logger;
        private readonly IProcessRequestService _processRequestService;
        private readonly CaptchaValidationService _captchaValidationService;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();

        public AuthController(IAuthenticationService authenticationService, ITokenService tokenService, IUserManagementService userManagementService, IDeviceManagementService deviceManagementService, IMailService mailService, ILogger<UserController> logger, IProcessRequestService processRequestService, CaptchaValidationService captchaValidationService)
        {
            _authenticationService = authenticationService;
            _tokenService = tokenService;
            _userManagementService = userManagementService;
            _deviceManagementService = deviceManagementService;
            _mailService = mailService;
            _logger = logger;
            _processRequestService = processRequestService;
            _captchaValidationService = captchaValidationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            if(userDto == null)
            {
                _logger.LogError("Received null userDto.");
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));
            }

            if (string.IsNullOrWhiteSpace(userDto.Password))
            {
                _logger.LogError("Password cannot be null.");
                return BadRequest(ApiResponse<string>.Fail("Password cannot be null or empty."));
            }

            _logger.LogInformation("Register attempt for user: {Email}", userDto.Email);

            if (!await _captchaValidationService.ValidateCaptchaAsync(userDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", userDto.Email);
                return BadRequest(ApiResponse<string>.Fail("Invalid CAPTCHA token."));
            }

            try
            {
                var (deviceInfo, ipAddress) = _processRequestService.GetRequestInfo();

                if(ipAddress == null)
                {
                    _logger.LogWarning("Failed to retrieve IP address for user: {Email}", userDto.Email);
                    return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));
                }

                var result = await _authenticationService.RegisterUserAsync(userDto, deviceInfo, ipAddress);
                if (result)
                {
                    _logger.LogInformation("Register successful for user: {Email}", userDto.Email);

                    return Ok(ApiResponse<string>.Ok("Register successful. Please check your email for confirmation."));
                }
            }
            catch(InvalidOperationException ex)
            {
                if(ex.Message == "Violation of service rules. All user accounts have been blocked.")
                {
                    _logger.LogWarning("Register failed for user: {Email} due to violation of service rules.", userDto.Email);
                    return BadRequest(ApiResponse<string>.Fail("Violation of service rules. All user accounts have been blocked."));
                }
                else if(ex.Message == "The account awaits confirmation. Follow the link in the email.")
                {
                    _logger.LogWarning("User account: {Email} expects confirmation by email.", userDto.Email);
                    return BadRequest(ApiResponse<string>.Fail("The account awaits confirmation. Follow the link in the email."));
                }

                _logger.LogWarning("Register failed for user: {Email} due to error: {Error}", userDto.Email, ex.Message);
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError("Register failed for user: {Email} due to error: {Error}", userDto.Email, ex.Message);
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }

            return BadRequest(ApiResponse<string>.Fail("Unknown error occured during registration."));
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            if (!await _captchaValidationService.ValidateCaptchaAsync(dto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for confirmation token: {Token}", dto.Token);
                return BadRequest(ApiResponse<string>.Fail("Invalid CAPTCHA token."));
            }

            try
            {
                var result = await _authenticationService.ConfirmEmailAsync(dto.Token);

                return result switch
                {
                    ConfirmTokenResult.Success => Ok(ApiResponse<string>.Ok("Email confirmed successfully.")),
                    ConfirmTokenResult.TokenNotFound => BadRequest(ApiResponse<string>.Fail("Invalid confirmation token.")),
                    ConfirmTokenResult.TokenUsed => BadRequest(ApiResponse<string>.Fail("This confirmation link has already been used.")),
                    ConfirmTokenResult.TokenExpired => BadRequest(ApiResponse<string>.Fail("Token expired. A new confirmation link has been sent to your email.")),
                    _ => StatusCode(500, ApiResponse<string>.Fail("Unknown error occurred."))
                };
            }
            catch(InvalidDataException ex)
            {
                _logger.LogWarning($"Invalid or expired token error occured: {ex.Message}");
                return BadRequest(ApiResponse<string>.Fail("Invalid or expired token."));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Email confirmation failed: {ex.Message}");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {            
            if(!await _captchaValidationService.ValidateCaptchaAsync(loginDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", loginDto.Email);
                return BadRequest(ApiResponse<string>.Fail("Invalid CAPTCHA token."));
            }

            try
            {
                var (deviceInfo, ipAddress) = _processRequestService.GetRequestInfo();
                if (ipAddress == null)
                {
                    _logger.LogWarning("Failed to retrieve IP address for user: {Email}", loginDto.Email);
                    return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));
                }

                var result = await _authenticationService.LoginUserAsync(loginDto, deviceInfo, ipAddress);

                if(result.AccessToken == null && result.RefreshToken == null && result.Message != null && result.SessionKey != null)
                {
                    _logger.LogInformation("Authorization from a new location for the user: {Email}", loginDto.Email);
                    return Ok(ApiResponse<LoginResponse>.Ok(result));
                }

                if (result.AccessToken == null || result.RefreshToken == null)
                {
                    _logger.LogWarning("Login failed for user: {Email}.", loginDto.Email);
                    return Unauthorized(ApiResponse<string>.Fail("Invalid credentials."));
                }

                return Ok(ApiResponse<LoginResponse>.Ok(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"Login failed for user: {loginDto.Email}. Reason: {ex.Message}");
                if (ex.Message == "Invalid password.")
                {
                    return Unauthorized(ApiResponse<string>.Fail("Invalid credentials."));
                }
                else if (ex.Message == "Please activate your account by following the link sent to your email.")
                {
                    return Unauthorized(ApiResponse<string>.Fail("Please activate your account by following the link sent to your email."));
                }
                return Unauthorized(ApiResponse<string>.Fail("User is banned."));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Login failed for user: {loginDto.Email}. Reason: {ex.Message}");
                return Unauthorized(ApiResponse<string>.Fail("User not found."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", loginDto.Email);
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPost("confirm-email-code")]
        public async Task<IActionResult> ConfirmEmailCode([FromBody] ConfirmCodeDto dto)
        {
            try
            {
                var result = await _authenticationService.ConfirmEmailCodeAsync(dto.Code, dto.SessionKey);
                return Ok(ApiResponse<LoginResponse>.Ok(result));
            }
            catch(UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Email code confirmation failed: {Message}", ex.Message);
                return Unauthorized(ApiResponse<string>.Fail(ex.Message));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during email code confirmation");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPost("confirm-2fa-code")]
        public async Task<IActionResult> ConfirmTwoFactorCode([FromBody] ConfirmCodeDto dto)
        {
            try
            {
                var result = await _authenticationService.ConfirmTwoFactorCodeAsync(dto.Code, dto.SessionKey);
                return Ok(ApiResponse<LoginResponse>.Ok(result));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("2FA code confirmation failed: {Message}", ex.Message);
                return Unauthorized(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during 2FA code confirmation");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPatch("password")]
        public async Task<IActionResult> RecoverPassword([FromBody] PasswordRecoveryDto recoveryDto)
        {
            if (!await _captchaValidationService.ValidateCaptchaAsync(recoveryDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for recovery token: {Token}", recoveryDto.RecoveryToken);
                return BadRequest(ApiResponse<string>.Fail("Invalid CAPTCHA token."));
            }

            if (recoveryDto.Password == null || recoveryDto.RecoveryToken == null)
            {
                _logger.LogWarning("Password or token was null.");
                return BadRequest(ApiResponse<string>.Fail("Password and recovery token cannot be null."));
            }

            try
            {
                var result = await _authenticationService.RecoverPassword(recoveryDto.RecoveryToken, recoveryDto.Password);

                return result switch
                {
                    RecoverPasswordResult.Success => Ok(ApiResponse<string>.Ok("Password changed successfully.")),
                    RecoverPasswordResult.Failed => BadRequest(ApiResponse<string>.Fail("Failed to change user password.")),
                    RecoverPasswordResult.UserNotFound => BadRequest(ApiResponse<string>.Fail("User not found.")),
                    RecoverPasswordResult.TokenNotFound => BadRequest(ApiResponse<string>.Fail("Invalid recovery token.")),
                    RecoverPasswordResult.TokenUsed => BadRequest(ApiResponse<string>.Fail("This link has already been used.")),
                    RecoverPasswordResult.TokenExpired => BadRequest(ApiResponse<string>.Fail("Token expired.")),
                    _ => StatusCode(500, ApiResponse<string>.Fail("Unknown error occurred."))
                };
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning("Invalid or expired token: {Message}", ex.Message);
                return BadRequest(ApiResponse<string>.Fail("Invalid or expired token."));
            }
            catch(Exception ex)
            {
                _logger.LogError("Unexpected error: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("Attempt to refresh token with an empty token.");
                return BadRequest(ApiResponse<string>.Fail("Refresh token is required."));
            }

            if (!ProcessingTokens.TryAdd(request.RefreshToken, true))
            {
                _logger.LogWarning("Refresh token request already in progress: {RefreshToken}", request.RefreshToken);
                return Conflict(ApiResponse<string>.Fail("Token refresh already in progress."));
            }

            try
            {
                var tokens = await _tokenService.RefreshAccessTokenAsync(request.RefreshToken);

                return Ok(new 
                { 
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refreshing token for refreshToken: {RefreshToken}", request.RefreshToken);
                return Unauthorized(ApiResponse<string>.Fail(ex.Message));
            }
            finally
            {
                ProcessingTokens.TryRemove(request.RefreshToken, out _);
            }
        }
    }
}
