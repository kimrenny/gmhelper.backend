using MatHelper.API.Common;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sprache;
using System.Collections.Concurrent;

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;
        private readonly IClientInfoService _infoService;
        private readonly ICaptchaValidationService _captchaValidationService;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();

        public AuthController(IAuthenticationService authenticationService, ITokenService tokenService, ILogger<AuthController> logger, IClientInfoService infoService, ICaptchaValidationService captchaValidationService)
        {
            _authenticationService = authenticationService;
            _tokenService = tokenService;
            _logger = logger;
            _infoService = infoService;
            _captchaValidationService = captchaValidationService;
        }

        [HttpPost("register/code")]
        public async Task<IActionResult> InitRegister([FromBody] RegisterRequestDto userDto)
        {
            if(userDto == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));
            }

            if (!await _captchaValidationService.ValidateCaptchaAsync(userDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", userDto.Email);
                return BadRequest(ApiResponse<string>.Fail("Invalid CAPTCHA token."));
            }

            try
            {
                var (deviceInfo, ipAddress) = _infoService.GetRequestInfo(HttpContext);

                if(ipAddress == null)
                {
                    return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));
                }

                var emailCode = await _authenticationService.InitRegisterUserAsync(userDto, deviceInfo, ipAddress);
                if (emailCode != null)
                {
                    return Ok(ApiResponse<string>.Ok("Verification code sent to email."));
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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            if (userDto == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));
            }

            if (userDto.Token == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Verification token is required."));
            }

            try
            {
                var (deviceInfo, ipAddress) = _infoService.GetRequestInfo(HttpContext);

                if (ipAddress == null)
                {
                    return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));
                }

                var result = await _authenticationService.RegisterUserAsync(userDto, deviceInfo, ipAddress);
                if (result == ConfirmTokenResult.Success)
                {
                    return Ok(ApiResponse<string>.Ok("Register successful."));
                }
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "Violation of service rules. All user accounts have been blocked.")
                {
                    _logger.LogWarning("Register failed for user: {Email} due to violation of service rules.", userDto.Email);
                    return BadRequest(ApiResponse<string>.Fail("Violation of service rules. All user accounts have been blocked."));
                }
                else if (ex.Message == "The account awaits confirmation. Follow the link in the email.")
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
                var (deviceInfo, ipAddress) = _infoService.GetRequestInfo(HttpContext);
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

                Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = result.RefreshTokenExpiration
                });

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
                else if (ex.Message == "IP temporarily blocked due to multiple failed login attempts.")
                {
                    return Unauthorized(ApiResponse<string>.Fail("IP temporarily blocked due to multiple failed login attempts."));
                }

                return Unauthorized(ApiResponse<string>.Fail("User is banned."));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Login failed for user: {loginDto.Email}. Reason: {ex.Message}");
                return Unauthorized(ApiResponse<string>.Fail("Invalid credentials."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", loginDto.Email);
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPost("email/confirm/code")]
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

        [HttpPost("2fa/confirm")]
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

        [HttpPost("password/recover/request")]
        public async Task<IActionResult> SendRecoveryPasswordEmail([FromBody] PasswordRecoveryEmailDto recoveryDto)
        {
            if (!await _captchaValidationService.ValidateCaptchaAsync(recoveryDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", recoveryDto.Email);
                return BadRequest(ApiResponse<string>.Fail("Invalid CAPTCHA token."));
            }

            try
            {
                await _authenticationService.SendRecoverPasswordLinkAsync(recoveryDto.Email);

                
                return Ok(ApiResponse<string>.Ok("If an account with this email exists, a recovery email has been sent."));
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning($"Invalid or expired token error occured: {ex.Message}");
                return Ok(ApiResponse<string>.Ok("If an account with this email exists, a recovery email has been sent."));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Email confirmation failed: {ex.Message}");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPost("password/recover")]
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

        [HttpPost("token/refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Attempt to refresh token with an empty token.");
                return BadRequest(ApiResponse<string>.Fail("Refresh token is required."));
            }

            if (!ProcessingTokens.TryAdd(refreshToken, true))
            {
                _logger.LogWarning("Refresh token request already in progress: {RefreshToken}", refreshToken);
                return Conflict(ApiResponse<string>.Fail("Token refresh already in progress."));
            }

            try
            {
                var tokens = await _tokenService.RefreshAccessTokenAsync(refreshToken);

                if (tokens.AccessToken == null || tokens.RefreshToken == null)
                {
                    _logger.LogWarning("Refresh failed for user.");
                    return Unauthorized(ApiResponse<string>.Fail("Invalid credentials."));
                }

                Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = tokens.RefreshTokenExpiration
                });

                return Ok(new LoginResponse
                { 
                    AccessToken = tokens.AccessToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refreshing token for refreshToken.");
                return Unauthorized(ApiResponse<string>.Fail(ex.Message));
            }
            finally
            {
                ProcessingTokens.TryRemove(refreshToken, out _);
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            Response.Cookies.Delete("refreshToken");

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return BadRequest(ApiResponse<string>.Fail("Refresh token is required."));
            }

            try
            {
                var result = await _tokenService.DisableRefreshToken(refreshToken);

                if (!result)
                {
                    return BadRequest(ApiResponse<string>.Fail("Token could not be deactivated."));
                }

                return Ok(ApiResponse<string>.Ok("Logged out successfully."));
            }
            catch (Exception ex) 
            {
                return Unauthorized(ApiResponse<string>.Fail(ex.Message));
            }
        }
    }
}
