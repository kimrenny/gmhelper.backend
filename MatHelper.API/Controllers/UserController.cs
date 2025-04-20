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
    public class UserController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ITokenService _tokenService;
        private readonly IUserManagementService _userManagementService;
        private readonly IDeviceManagementService _deviceManagementService;
        private readonly ILogger<UserController> _logger;
        private readonly IProcessRequestService _processRequestService;
        private readonly CaptchaValidationService _captchaValidationService;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();

        public UserController(IAuthenticationService authenticationService, ITokenService tokenService, IUserManagementService userManagementService, IDeviceManagementService deviceManagementService, ILogger<UserController> logger, IProcessRequestService processRequestService, CaptchaValidationService captchaValidationService)
        {
            _authenticationService = authenticationService;
            _tokenService = tokenService;
            _userManagementService = userManagementService;
            _deviceManagementService = deviceManagementService;
            _logger = logger;
            _processRequestService = processRequestService;
            _captchaValidationService = captchaValidationService;
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

            var devices = await _deviceManagementService.GetLoggedDevicesAsync(Guid.Parse(userId));
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
                var result = await _deviceManagementService.RemoveDeviceAsync(Guid.Parse(userId), request.UserAgent, request.Platform, request.IpAddress, token);
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
