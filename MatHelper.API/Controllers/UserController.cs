using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.OpenApi.Validations;

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
            _logger.LogInformation("Register attempt for user: {Email}", userDto.Email);

            if (!await _captchaValidationService.ValidateCaptchaAsync(userDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", userDto.Email);
                return BadRequest("Invalid CAPTCHA token.");
            }

            if (!await _userService.RegisterUserAsync(userDto))
            {
                _logger.LogInformation("Register failed for user: {Email}", userDto.Email);
                return BadRequest("User with this email already exists.");
            }

            _logger.LogInformation("Register successful for user: {Email}", userDto.Email);
            return Ok();
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
                var token = await _userService.LoginUserAsync(loginDto);
                if (token == null)
                {
                    _logger.LogWarning("Login failed for user: {Email}", loginDto.Email);
                    return Unauthorized("Invalid credentials.");
                }

                _logger.LogInformation("Login successful for user: {Email}", loginDto.Email);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", loginDto.Email);
                return StatusCode(500, "Internal server error.");
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
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            try
            {
                var accessToken = await _userService.RefreshAccessTokenAsync(refreshToken);
                return Ok(new { AccessToken = accessToken });
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return BadRequest("Invalid file.");

            using var memoryStream = new MemoryStream();
            await avatar.CopyToAsync(memoryStream);
            var avatarBytes = memoryStream.ToArray();

            await _userService.SaveUserAvatarAsync(User.Identity.Name, avatarBytes);
            return Ok("Avatar uploaded successfully.");
        }

        [HttpGet("avatar")]
        public async Task<IActionResult> GetAvatar()
        {
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userId))
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
                var userId = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(userId))
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
            catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
