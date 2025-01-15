using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.OpenApi.Validations;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;

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
        private readonly CaptchaValidationService _captchaValidationService;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();

        public UserController(IAuthenticationService authenticationService, ITokenService tokenService, IUserManagementService userManagementService, ILogger<UserController> logger, CaptchaValidationService captchaValidationService)
        {
            _authenticationService = authenticationService;
            _tokenService = tokenService;
            _userManagementService = userManagementService;
            _logger = logger;
            _captchaValidationService = captchaValidationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            if(userDto == null)
            {
                _logger.LogError("Received null userDto.");
                return BadRequest("Invalid data.");
            }

            _logger.LogInformation("Register attempt for user: {Email}", userDto.Email);

            if (!await _captchaValidationService.ValidateCaptchaAsync(userDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", userDto.Email);
                return BadRequest("Invalid CAPTCHA token.");
            }

            try
            {
                var result = await _authenticationService.RegisterUserAsync(userDto);
                if (result)
                {
                    _logger.LogInformation("Register successful for user: {Email}", userDto.Email);
                    return Ok();
                }
            }
            catch(InvalidOperationException ex)
            {
                if(ex.Message == "Violation of service rules. All user accounts have been blocked.")
                {
                    _logger.LogWarning("Register failed for user: {Email} due to violation of service rules.", userDto.Email);
                    return BadRequest("Violation of service rules. All user accounts have been blocked.");
                }

                _logger.LogWarning("Register failed for user: {Email} due to error: {Error}", userDto.Email, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Register failed for user: {Email} due to error: {Error}", userDto.Email, ex.Message);
                return BadRequest(ex.Message);
            }

            return BadRequest("Unknown error occured during registration.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for user: {Email}", loginDto.Email);
            
            if(!await _captchaValidationService.ValidateCaptchaAsync(loginDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", loginDto.Email);
                return BadRequest("Invalid CAPTCHA token.");
            }

            try
            {
                var (accessToken, refreshToken) = await _authenticationService.LoginUserAsync(loginDto);
                if (accessToken == null || refreshToken == null)
                {
                    _logger.LogWarning($"Login failed for user: {loginDto.Email}.");
                    return Unauthorized("Invalid credentials.");
                }

                _logger.LogInformation("Login successful for user: {Email}", loginDto.Email);
                return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Login failed for user: {loginDto.Email}. Reason: {ex.Message}");
                return Unauthorized("User not found.");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"Login failed for user: {loginDto.Email}. Reason: {ex.Message}");
                return Unauthorized("User is banned.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", loginDto.Email);
                return StatusCode(500, "An unexpected error occured.");
            }
        }

        [HttpPost("password-recovery")]
        public async Task<IActionResult> RecoverPassword([FromBody] PasswordRecoveryDto recoveryDto)
        {
            if (!await _captchaValidationService.ValidateCaptchaAsync(recoveryDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", recoveryDto.Email);
                return BadRequest("Invalid CAPTCHA token.");
            }

            if (!await _authenticationService.RecoverPasswordAsync(recoveryDto))
                return NotFound("User not found.");

            return Ok("Password recovery instructions sent.");
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("Attempt to refresh token with an empty token.");
                return BadRequest("Refresh token is required.");
            }

            if (!ProcessingTokens.TryAdd(request.RefreshToken, true))
            {
                _logger.LogWarning("Refresh token request already in progress: {RefreshToken}", request.RefreshToken);
                return Conflict("Token refresh already in progress.");
            }

            try
            {
                _logger.LogInformation("Attempting to refresh token.");
                var tokens = await _tokenService.RefreshAccessTokenAsync(request.RefreshToken);
                _logger.LogInformation("Token refreshed successfully for refreshToken: {RefreshToken}", request.RefreshToken);

                return Ok(new 
                { 
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refreshing token for refreshToken: {RefreshToken}", request.RefreshToken);
                return Unauthorized(ex.Message);
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
                return Unauthorized(new { message = "User token is not active." });
            }

            if (avatar == null || avatar.Length == 0)
                return BadRequest("Invalid file.");

            try
            {
                using var memoryStream = new MemoryStream();
                await avatar.CopyToAsync(memoryStream);
                var avatarBytes = memoryStream.ToArray();

                await _userManagementService.SaveUserAvatarAsync(User.Identity.Name, avatarBytes);
                return Ok(new { message = "Avatar uploaded successfully." });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
            
        }

        [HttpGet("avatar")]
        [Authorize]
        public async Task<IActionResult> GetAvatar()
        {
            var token = Request.Headers["Authorization"].ToString().Split(" ").Last();
            if (await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(new { message = "User token is not active." });
            }

            var userIdString = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Invalid token.");
            }

            var avatar = await _userManagementService.GetUserAvatarAsync(userId);
            if(avatar == null || avatar.Length == 0)
            {
                return NotFound("Avatar not found.");
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
                return Unauthorized(new { message = "Authorization header is missing or invalid" });
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            if (await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(new { message = "User token is not active." });
            }

            try
            {
                var userIdString = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized("Invalid token.");
                }

                var user = await _userManagementService.GetUserDetailsAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                return Ok(new
                {
                    nickname = user.Username,
                    avatar = user.Avatar,
                });
            }
            catch(InvalidDataException ex)
            {
                return Unauthorized("Invalid data.");
            }
            catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("devices")]
        [Authorize]
        public async Task<IActionResult> GetLoggedDevices()
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Authorization header is missing or invalid" });
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            if (await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(new { message = "User token is not active." });
            }

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized("User ID is not available in the token.");
            }

            var devices = await _userManagementService.GetLoggedDevicesAsync(Guid.Parse(userId));
            if(devices == null || !devices.Any())
            {
                return NotFound("No devices found for this user.");
            }

            return Ok(devices);
        }

        [HttpPut("update")]
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
                return Unauthorized(new { message = "User token is not active." });
            }

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId)) {
                return Unauthorized(new { message = "User ID is not available in the token." });
            }

            try
            {
                await _userManagementService.UpdateUserAsync(Guid.Parse(userId), request);
                return Ok(new { message = "User data updated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error updating user data.");
                return StatusCode(500, new { message = "An unexpected error occured." });
            }
        }

        [HttpPatch("devices/deactivate")]
        [Authorize]
        public async Task<IActionResult> RemoveDevice([FromBody] RemoveDeviceRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Authorization header is missing or invalid" });
            }
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            if (await _tokenService.IsTokenDisabled(token))
            {
                return Unauthorized(new { message = "User token is not active." });
            }

            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "User ID is not available in the token." });
            }

            try
            {
                var result = await _userManagementService.RemoveDeviceAsync(Guid.Parse(userId), request.UserAgent, request.Platform);
                if(result.ToString() == "User not found." || result.ToString() == "Device not found or inactive." || result.ToString() == "An unexpected error occured.")
                {
                    return BadRequest(new { message = result });
                }

                return Ok(new { message = result });

            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error while removing device for user {UserId}", userId);
                return StatusCode(500, new { message = "An unexpected error occured." });
            }
        }
    }
}
