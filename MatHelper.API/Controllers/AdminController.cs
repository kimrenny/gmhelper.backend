using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.OpenApi.Validations;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using MatHelper.DAL.Repositories;

namespace MatHelper.API.Controllers
{
    [Authorize(Roles = "Admin, Owner")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ITokenService _tokenService;
        private readonly ISecurityService _securityService;
        private readonly ILogger<UserController> _logger;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();
        private readonly IRequestLogService _logService;

        public AdminController(IAdminService adminService, ITokenService tokenService, ISecurityService securityService, ILogger<UserController> logger, IRequestLogService logService)
        {
            _adminService = adminService;
            _tokenService = tokenService;
            _securityService = securityService;
            _logger = logger;
            _logService = logService;
        }

        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var token = await _tokenService.ExtractTokenAsync(Request);
                if (token == null)
                {
                    return Unauthorized("Authorization header is missing or invalid");
                }

                if (await _tokenService.IsTokenDisabled(token))
                {
                    return Unauthorized("User token is not active.");
                }

                var userId = await _tokenService.GetUserIdFromTokenAsync(User);
                if (userId == null)
                {
                    return Unauthorized("User ID is not available in the token.");
                }

                if (!await _tokenService.HasAdminPermissionsAsync(userId.Value))
                {
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
                var token = await _tokenService.ExtractTokenAsync(Request);
                if (token == null)
                {
                    return Unauthorized("Authorization header is missing or invalid");
                }

                if (await _tokenService.IsTokenDisabled(token))
                {
                    return Unauthorized("User token is not active.");
                }

                var userId = await _tokenService.GetUserIdFromTokenAsync(User);
                if (userId == null)
                {
                    return Unauthorized("User ID is not available in the token.");
                }

                if (!await _tokenService.HasAdminPermissionsAsync(userId.Value))
                {
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
                var token = await _tokenService.ExtractTokenAsync(Request);
                if (token == null)
                {
                    return Unauthorized("Authorization header is missing or invalid");
                }

                if (await _tokenService.IsTokenDisabled(token))
                {
                    return Unauthorized("User token is not active.");
                }

                var userId = await _tokenService.GetUserIdFromTokenAsync(User);
                if (userId == null)
                {
                    return Unauthorized("User ID is not available in the token.");
                }

                if (!await _tokenService.HasAdminPermissionsAsync(userId.Value))
                {
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
                var token = await _tokenService.ExtractTokenAsync(Request);
                if (token == null)
                {
                    return Unauthorized("Authorization header is missing or invalid");
                }

                if (await _tokenService.IsTokenDisabled(token))
                {
                    return Unauthorized("User token is not active.");
                }

                var userId = await _tokenService.GetUserIdFromTokenAsync(User);
                if (userId == null)
                {
                    return Unauthorized("User ID is not available in the token.");
                }

                if (!await _tokenService.HasAdminPermissionsAsync(userId.Value))
                {
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
                var token = await _tokenService.ExtractTokenAsync(Request);
                if (token == null)
                {
                    return Unauthorized("Authorization header is missing or invalid");
                }

                if (await _tokenService.IsTokenDisabled(token))
                {
                    return Unauthorized("User token is not active.");
                }

                var userId = await _tokenService.GetUserIdFromTokenAsync(User);
                if (userId == null)
                {
                    return Unauthorized("User ID is not available in the token.");
                }

                if (!await _tokenService.HasAdminPermissionsAsync(userId.Value))
                {
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

        [HttpGet("dashboard/tokens")]
        [Authorize]
        public async Task<IActionResult> GetDashboardTokens()
        {
            try
            {
                var token = await _tokenService.ExtractTokenAsync(Request);
                if(token == null)
                {
                    return Unauthorized("Authorization header is missing or invalid");
                }

                if (await _tokenService.IsTokenDisabled(token))
                {
                    return Unauthorized("User token is not active.");
                }

                var userId = await _tokenService.GetUserIdFromTokenAsync(User);
                if (userId == null)
                {
                    return Unauthorized("User ID is not available in the token.");
                }

                if (!await _tokenService.HasAdminPermissionsAsync(userId.Value))
                {
                    return Forbid("User does not have permissions.");
                }

                var activeUsers = await _adminService.GetDashboardTokensAsync();

                return Ok(activeUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("request-stats")]
        public async Task<IActionResult> GetRequestStats()
        {
            var stats = await _logService.GetRequestStats();
            return Ok(stats);
        }
    }
}
