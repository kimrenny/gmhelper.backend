using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.OpenApi.Validations;
using Microsoft.AspNetCore.Authorization;

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly CaptchaValidationService _captchaValidationService;

        public UserController(IUserService userService, ILogger<UserController> logger, CaptchaValidationService captchaValidationService)
        {
            _userService = userService;
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
                var result = await _userService.RegisterUserAsync(userDto);
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
                var (accessToken, refreshToken) = await _userService.LoginUserAsync(loginDto);
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

            if (!await _userService.RecoverPasswordAsync(recoveryDto))
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

            try
            {
                _logger.LogInformation("Attempting to refresh token.");
                var tokens = await _userService.RefreshAccessTokenAsync(request.RefreshToken);
                _logger.LogInformation("Token refreshed successfully for refreshToken: {RefreshToken}", request.RefreshToken);

                return Ok(new { 
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refreshing token for refreshToken: {RefreshToken}", request.RefreshToken);
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return BadRequest("Invalid file.");

            try
            {
                using var memoryStream = new MemoryStream();
                await avatar.CopyToAsync(memoryStream);
                var avatarBytes = memoryStream.ToArray();

                await _userService.SaveUserAvatarAsync(User.Identity.Name, avatarBytes);
                return Ok(new { message = "Avatar uploaded successfully." });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
            
        }

        [HttpGet("avatar")]
        public async Task<IActionResult> GetAvatar()
        {
            var userIdString = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Invalid token.");
            }

            var avatar = await _userService.GetUserAvatarAsync(userId);
            if(avatar == null || avatar.Length == 0)
            {
                return NotFound("Avatar not found.");
            }

            return File(avatar, "image/jpeg");
        }

        [HttpGet("details")]
        public async Task<IActionResult> GetUserDetails()
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized("Invalid token.");
                }

                var user = await _userService.GetUserDetailsAsync(userId);
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
            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized("User ID is not available in the token.");
            }

            var devices = await _userService.GetLoggedDevicesAsync(Guid.Parse(userId));
            if(devices == null || !devices.Any())
            {
                return NotFound("No devices found for this user.");
            }

            return Ok(devices);
        }
    }
}
