using MatHelper.API.Common;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IUserManagementService _userManagementService;
        private readonly IDeviceManagementService _deviceManagementService;
        private readonly ILogger<UserController> _logger;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();

        public UserController(ITokenService tokenService, IUserManagementService userManagementService, IDeviceManagementService deviceManagementService, ILogger<UserController> logger)
        {
            _tokenService = tokenService;
            _userManagementService = userManagementService;
            _deviceManagementService = deviceManagementService;
            _logger = logger;
        }

        [HttpPost("avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
        {
            var token = _tokenService.ExtractTokenAsync(Request);

            if (token == null || await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            try
            {
                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
                }

                using var memoryStream = new MemoryStream();
                await avatar.CopyToAsync(memoryStream);
                var avatarBytes = memoryStream.ToArray();

                await _userManagementService.SaveUserAvatarAsync(userId.Value, avatarBytes);
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
            var token = _tokenService.ExtractTokenAsync(Request);

            if (token == null || await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            try
            {
                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
                }

                var avatar = await _userManagementService.GetUserAvatarAsync(userId.Value);
                if (avatar == null || avatar.Length == 0)
                {
                    return NotFound(ApiResponse<string>.Fail("Avatar not found."));
                }

                return File(avatar, "image/jpeg");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpGet("details")]
        [Authorize]
        public async Task<IActionResult> GetUserDetails()
        {
            var token = _tokenService.ExtractTokenAsync(Request);

            if (token == null || await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            try
            {
                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null)
                {
                    return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
                }

                var user = await _userManagementService.GetUserDetailsAsync(userId.Value);
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
            var token = _tokenService.ExtractTokenAsync(Request);

            if (token == null || await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));
            }

            var userId = await _tokenService.GetUserIdFromTokenAsync(token);
            if (userId == null)
            {
                return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
            }

            var devices = await _deviceManagementService.GetLoggedDevicesAsync(userId.Value);
            if(devices == null || !devices.Any())
            {
                return NotFound(ApiResponse<string>.Fail("No devices found for this user."));
            }

            return Ok(devices);
        }

        [HttpPatch("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserRequest request)
        {
            var token = _tokenService.ExtractTokenAsync(Request);

            if (token == null || await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));
            }

            try
            {
                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));

                await _userManagementService.UpdateUserAsync(userId.Value, request);
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

        [HttpPatch("profile/language")]
        [Authorize]
        public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
        {
            var token = _tokenService.ExtractTokenAsync(Request);

            if (token == null || await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));
            }

            try
            {
                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));

                if (!Enum.TryParse(typeof(LanguageType), request.Language, true, out var parsedLanguage) || !Enum.IsDefined(typeof(LanguageType), parsedLanguage))
                {
                    return BadRequest(ApiResponse<string>.Fail("Invalid language type."));
                }

                await _userManagementService.UpdateUserLanguageAsync(userId.Value, (LanguageType)parsedLanguage);
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
            var token = _tokenService.ExtractTokenAsync(Request);

            if (token == null || await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));
            }

            try
            {
                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));

                var result = await _deviceManagementService.RemoveDeviceAsync(userId.Value, request.UserAgent, request.Platform, request.IpAddress, token);
                if(result.ToString() == "User not found." || result.ToString() == "Device not found or inactive." || result.ToString() == "The current device cannot be deactivated." || result.ToString() == "An unexpected error occured.")
                {
                    return BadRequest(ApiResponse<string>.Fail(result));
                }

                return Ok(ApiResponse<string>.Ok(result));

            } 
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error while removing device for user.");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }
    }
}
