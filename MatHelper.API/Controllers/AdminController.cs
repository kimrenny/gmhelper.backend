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
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ITokenService _tokenService;
        private readonly ISecurityService _securityService;
        private readonly ILogger<UserController> _logger;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();

        public AdminController(IAdminService adminService, ITokenService tokenService, ISecurityService securityService, ILogger<UserController> logger)
        {
            _adminService = adminService;
            _tokenService = tokenService;
            _securityService = securityService;
            _logger = logger;
        }

        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                {
                    _logger.LogWarning("Authorization header is missing or invalid.");
                    return Unauthorized("Authorization header is missing or invalid");
                }
                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                if (await _tokenService.IsTokenDisabled(token))
                {
                    _logger.LogWarning("User token is not active.");
                    return Unauthorized("User token is not active.");
                }

                var userId = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID is not available in the token.");
                    return Unauthorized("User ID is not available in the token.");
                }

                _logger.LogInformation($"User ID found in token: {userId}");

                if (!await _securityService.HasAdminPermissions(Guid.Parse(userId)))
                {
                    _logger.LogWarning("User does not have admin permissions.");
                    return Forbid("User does not have permissions.");
                }

                var users = await _adminService.GetUsersAsync();
                if (users == null || !users.Any())
                {
                    _logger.LogError("Users data not found.");
                    return NotFound("Users data not found.");
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("users/action")]
        [Authorize]
        public async Task<IActionResult> ActionUser([FromBody] AdminActionDto adminActionDto)
        {
            if(adminActionDto == null)
            {
                return BadRequest("Invalid data.");
            }

            try
            {
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                {
                    _logger.LogWarning("Authorization header is missing or invalid.");
                    return Unauthorized("Authorization header is missing or invalid");
                }
                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                if (await _tokenService.IsTokenDisabled(token))
                {
                    _logger.LogWarning("User token is not active.");
                    return Unauthorized("User token is not active.");
                }

                var userId = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID is not available in the token.");
                    return Unauthorized("User ID is not available in the token.");
                }

                if (!await _securityService.HasAdminPermissions(Guid.Parse(userId)))
                {
                    _logger.LogWarning("User does not have admin permissions.");
                    return Forbid("User does not have permissions.");
                }

                await _adminService.ActionUserAsync(Guid.Parse(adminActionDto.Id), adminActionDto.Action);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("tokens")]
        [Authorize]
        public async Task<IActionResult> GetTokens()
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                {
                    _logger.LogWarning("Authorization header is missing or invalid.");
                    return Unauthorized("Authorization header is missing or invalid");
                }
                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                if (await _tokenService.IsTokenDisabled(token))
                {
                    _logger.LogWarning("User token is not active.");
                    return Unauthorized("User token is not active.");
                }

                var userId = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID is not available in the token.");
                    return Unauthorized("User ID is not available in the token.");
                }

                _logger.LogInformation($"User ID found in token: {userId}");

                if (!await _securityService.HasAdminPermissions(Guid.Parse(userId)))
                {
                    _logger.LogWarning("User does not have admin permissions.");
                    return Forbid("User does not have permissions.");
                }

                var tokens = await _adminService.GetTokensAsync();
                if (tokens == null || !tokens.Any())
                {
                    _logger.LogError("Users data not found.");
                    return NotFound("Users data not found.");
                }

                return Ok(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("tokens/action")]
        [Authorize]
        public async Task<IActionResult> ActionToken([FromBody] AdminActionDto adminActionDto)
        {
            if (adminActionDto == null)
            {
                return BadRequest("Invalid data.");
            }

            try
            {
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                {
                    _logger.LogWarning("Authorization header is missing or invalid.");
                    return Unauthorized("Authorization header is missing or invalid");
                }
                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                if (await _tokenService.IsTokenDisabled(token))
                {
                    _logger.LogWarning("User token is not active.");
                    return Unauthorized("User token is not active.");
                }

                var userId = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID is not available in the token.");
                    return Unauthorized("User ID is not available in the token.");
                }

                if (!await _securityService.HasAdminPermissions(Guid.Parse(userId)))
                {
                    _logger.LogWarning("User does not have admin permissions.");
                    return Forbid("User does not have permissions.");
                }

                await _adminService.ActionTokenAsync(adminActionDto.Id, adminActionDto.Action);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("dashboard/registrations")]
        [Authorize]
        public async Task<IActionResult> GetRegistrations()
        {
            try
            {
                var authorizationHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                {
                    _logger.LogWarning("Authorization header is missing or invalid.");
                    return Unauthorized("Authorization header is missing or invalid");
                }
                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                if (await _tokenService.IsTokenDisabled(token))
                {
                    _logger.LogWarning("User token is not active.");
                    return Unauthorized("User token is not active.");
                }

                var userId = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("User ID is not available in the token.");
                    return Unauthorized("User ID is not available in the token.");
                }

                _logger.LogInformation($"User ID found in token: {userId}");

                if (!await _securityService.HasAdminPermissions(Guid.Parse(userId)))
                {
                    _logger.LogWarning("User does not have admin permissions.");
                    return Forbid("User does not have permissions.");
                }

                var registrations = await _adminService.GetRegistrationsAsync();
                if (registrations == null || !registrations.Any())
                {
                    _logger.LogError("Users data not found.");
                    return NotFound("Users data not found.");
                }

                return Ok(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
