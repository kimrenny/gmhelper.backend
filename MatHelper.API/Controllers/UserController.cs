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

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ITokenService _tokenService;
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<UserController> _logger;
        private readonly IProcessRequestService _processRequestService;
        private readonly CaptchaValidationService _captchaValidationService;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();

        public UserController(IAuthenticationService authenticationService, ITokenService tokenService, IUserManagementService userManagementService, ILogger<UserController> logger, IProcessRequestService processRequestService, CaptchaValidationService captchaValidationService)
        {
            _authenticationService = authenticationService;
            _tokenService = tokenService;
            _userManagementService = userManagementService;
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
                    return Ok(ApiResponse<string>.Ok("Register successful."));
                }
            }
            catch(InvalidOperationException ex)
            {
                if(ex.Message == "Violation of service rules. All user accounts have been blocked.")
                {
                    _logger.LogWarning("Register failed for user: {Email} due to violation of service rules.", userDto.Email);
                    return BadRequest(ApiResponse<string>.Fail("Violation of service rules. All user accounts have been blocked."));
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
                var (deviceInfo, ipAddress) = _processRequestService.GetRequestInfo();
                if (ipAddress == null)
                {
                    _logger.LogWarning("Failed to retrieve IP address for user: {Email}", loginDto.Email);
                    return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));
                }

                var (accessToken, refreshToken) = await _authenticationService.LoginUserAsync(loginDto, deviceInfo, ipAddress);

                if (accessToken == null || refreshToken == null)
                {
                    _logger.LogWarning("Login failed for user: {Email}.", loginDto.Email);
                    return Unauthorized(ApiResponse<string>.Fail("Invalid credentials."));
                }

                return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Login failed for user: {loginDto.Email}. Reason: {ex.Message}");
                return Unauthorized(ApiResponse<string>.Fail("User not found."));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"Login failed for user: {loginDto.Email}. Reason: {ex.Message}");
                if(ex.Message == "Invalid password.")
                {
                    return Unauthorized(ApiResponse<string>.Fail("Invalid credentials."));
                }
                return Unauthorized(ApiResponse<string>.Fail("User is banned."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", loginDto.Email);
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPost("password-recovery")]
        public async Task<IActionResult> RecoverPassword([FromBody] PasswordRecoveryDto recoveryDto)
        {
            if (!await _captchaValidationService.ValidateCaptchaAsync(recoveryDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", recoveryDto.Email);
                return BadRequest(ApiResponse<string>.Fail("Invalid CAPTCHA token."));
            }

            if (!await _authenticationService.RecoverPasswordAsync(recoveryDto))
                return NotFound(ApiResponse<string>.Fail("User not found."));

            return Ok(ApiResponse<string>.Ok("Password recovery instructions sent."));
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

        [HttpPost("upload-avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
        {
            var token = Request.Headers["Authorization"].ToString().Split(" ").Last();
            if (await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            if (avatar == null || avatar.Length == 0)
                return BadRequest(ApiResponse<string>.Fail("Invalid file."));

            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<string>.Fail("User is not authenticated."));
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await avatar.CopyToAsync(memoryStream);
                var avatarBytes = memoryStream.ToArray();

                await _userManagementService.SaveUserAvatarAsync(userId, avatarBytes);
                return Ok(ApiResponse<string>.Ok("Avatar uploaded successfully."));
            }
            catch(Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
            }
            
        }

        [HttpGet("avatar")]
        [Authorize]
        public async Task<IActionResult> GetAvatar()
        {
            var token = Request.Headers["Authorization"].ToString().Split(" ").Last();
            if (await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            var userIdString = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(ApiResponse<string>.Fail("Invalid token."));
            }

            var avatar = await _userManagementService.GetUserAvatarAsync(userId);
            if(avatar == null || avatar.Length == 0)
            {
                return NotFound(ApiResponse<string>.Fail("Avatar not found."));
            }

            return File(avatar, "image/jpeg");
        }

        [HttpGet("details")]
        [Authorize]
        public async Task<IActionResult> GetUserDetails()
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                this._logger.LogError("Authorization header is missing or invalid {authorizationHeader}.", authorizationHeader);
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            if (await _tokenService.IsTokenDisabled(token))
            {
                this._logger.LogError("User token is not active: {token}", token);
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            try
            {
                var userIdString = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    this._logger.LogError("Invalid token: {token}", token);
                    return Unauthorized(ApiResponse<string>.Fail("Invalid token."));
                }

                var user = await _userManagementService.GetUserDetailsAsync(userId);
                if (user == null)
                {
                    this._logger.LogError("User not found for token: {token}", token);
                    return NotFound(ApiResponse<string>.Fail("User not found."));
                }

                return Ok(ApiResponse<UserDetails>.Ok(user));
            }
            catch(InvalidDataException ex)
            {
                this._logger.LogError("Invalid data: {ex}", ex);
                return Unauthorized(ApiResponse<string>.Fail("Invalid data"));
            }
            catch (Exception ex) {
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpGet("devices")]
        [Authorize]
        public async Task<IActionResult> GetLoggedDevices()
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            if (await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
            }

            var devices = await _userManagementService.GetLoggedDevicesAsync(Guid.Parse(userId));
            if(devices == null || !devices.Any())
            {
                return NotFound(ApiResponse<string>.Fail("No devices found for this user."));
            }

            return Ok(devices);
        }

        [HttpPatch("update-user")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Authorization header is missing or invalid" });
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            if (await _tokenService.IsTokenDisabled(token))
            {
                return Forbid();
            }

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId)) {
                return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
            }

            try
            {
                await _userManagementService.UpdateUserAsync(Guid.Parse(userId), request);
                return Ok(ApiResponse<string>.Ok("User data updated successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error updating user data.");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPatch("update-language")]
        [Authorize]
        public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            if (await _tokenService.IsTokenDisabled(token))
            {
                return Forbid(ApiResponse<string>.Fail("Token is disabled.").Message ?? "Token is disabled.");
            }

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
            }

            if(!Enum.TryParse(typeof(LanguageType), request.Language, true, out var parsedLanguage) || !Enum.IsDefined(typeof(LanguageType), parsedLanguage))
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid language type."));
            }

            try
            {
                await _userManagementService.UpdateUserLanguageAsync(Guid.Parse(userId), (LanguageType)parsedLanguage);
                return Ok(ApiResponse<string>.Fail("Language updated successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user data.");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }

        [HttpPatch("devices/deactivate")]
        [Authorize]
        public async Task<IActionResult> RemoveDevice([FromBody] RemoveDeviceRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            if (await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
            }

            try
            {
                var result = await _userManagementService.RemoveDeviceAsync(Guid.Parse(userId), request.UserAgent, request.Platform, request.IpAddress, token);
                if(result.ToString() == "User not found." || result.ToString() == "Device not found or inactive." || result.ToString() == "The current device cannot be deactivated." || result.ToString() == "An unexpected error occured.")
                {
                    return BadRequest(ApiResponse<string>.Fail(result));
                }

                return Ok(ApiResponse<string>.Ok(result));

            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error while removing device for user {UserId}", userId);
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }
    }
}
