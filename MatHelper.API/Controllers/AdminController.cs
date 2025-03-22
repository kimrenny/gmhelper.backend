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
using TokenValidationResult = MatHelper.CORE.Enums.TokenValidationResult;
using MatHelper.API.Common;

namespace MatHelper.API.Controllers
{
    [Authorize(Roles = "Admin, Owner")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IAdminSettingsService _adminSettingsService;
        private readonly ITokenService _tokenService;
        private readonly ISecurityService _securityService;
        private readonly ILogger<UserController> _logger;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();
        private readonly IRequestLogService _logService;

        public AdminController(IAdminService adminService, IAdminSettingsService AdminSettingsService, ITokenService tokenService, ISecurityService securityService, ILogger<UserController> logger, IRequestLogService logService)
        {
            _adminService = adminService;
            _adminSettingsService = AdminSettingsService;
            _tokenService = tokenService;
            _securityService = securityService;
            _logger = logger;
            _logService = logService;
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                var users = await _adminService.GetUsersAsync();
                if (users == null || !users.Any())
                {
                    _logger.LogError("Users data not found.");
                    return new ObjectResult(ApiResponse<string>.Fail("Users data not found.")) { StatusCode = 404 };
                }

                return Ok(ApiResponse<List<AdminUserDto>>.Ok(users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return new ObjectResult(ApiResponse<string>.Fail("Internal server error.")) { StatusCode = 500 };
            }
        }

        [HttpPut("users/action")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> ActionUser([FromBody] AdminActionDto adminActionDto)
        {
            if(adminActionDto == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));
            }

            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                await _adminService.ActionUserAsync(Guid.Parse(adminActionDto.Id), adminActionDto.Action);
                return Ok(ApiResponse<string>.Ok("Action performed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("tokens")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> GetTokens()
        {
            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                var tokens = await _adminService.GetTokensAsync();
                if (tokens == null || !tokens.Any())
                {
                    _logger.LogError("Users data not found.");
                    return NotFound(ApiResponse<string>.Fail("Users data not found."));
                }

                return Ok(ApiResponse<List<TokenDto>>.Ok(tokens));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpPut("tokens/action")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> ActionToken([FromBody] AdminActionDto adminActionDto)
        {
            if (adminActionDto == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));
            }

            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                await _adminService.ActionTokenAsync(adminActionDto.Id, adminActionDto.Action);
                return Ok(ApiResponse<string>.Ok("Token action performed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("dashboard/registrations")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> GetRegistrations()
        {
            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                var registrations = await _adminService.GetRegistrationsAsync();
                if (registrations == null || !registrations.Any())
                {
                    _logger.LogError("Users data not found.");
                    return NotFound(ApiResponse<string>.Fail("Registrations data not found."));
                }

                return Ok(ApiResponse<List<RegistrationsDto>>.Ok(registrations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("dashboard/tokens")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> GetDashboardTokens()
        {
            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                var activeUsers = await _adminService.GetDashboardTokensAsync();

                return Ok(ApiResponse<DashboardTokensDto>.Ok(activeUsers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("country-rating")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> GetUsersByCountry()
        {
            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                var userCountryStats = await _adminService.GetUsersByCountryAsync();
                
                if(userCountryStats == null || !userCountryStats.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("No users found."));
                }

                return Ok(ApiResponse<List<CountryStatsDto>>.Ok(userCountryStats));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occured while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }


        [HttpGet("role-stats")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> GetRoleStats()
        {
            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                var roleStats = await _adminService.GetRoleStatsAsync();

                if (roleStats == null || !roleStats.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("No stats found."));
                }

                return Ok(ApiResponse<List<RoleStatsDto>>.Ok(roleStats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("block-stats")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> GetBlockStats()
        {
            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);
                if (validationResult != TokenValidationResult.Valid)
                {
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions => Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
                    };

                }

                var blockStats = await _adminService.GetBlockStatsAsync();

                if (blockStats == null || !blockStats.Any())
                {
                    return NotFound(ApiResponse<string>.Fail("No stats found."));
                }

                return Ok(ApiResponse<List<BlockStatsDto>>.Ok(blockStats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("settings")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> GetAdminSettings()
        {
            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("User ID is missing in the token.");
                return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
            }

            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);

                if (validationResult != TokenValidationResult.Valid)
                {
                    _logger.LogWarning("Token validation failed: {ValidationResult}", validationResult);
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions =>
                            Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occurred.")) { StatusCode = 500 }
                    };
                }

                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    _logger.LogError("Failed to parse User ID: {UserId}", userId);
                    return BadRequest(ApiResponse<string>.Fail("Invalid User ID format."));
                }

                var settings = await _adminSettingsService.GetOrCreateAdminSettingsAsync(parsedUserId);
                if (settings == null)
                {
                    _logger.LogWarning("No settings found for User ID: {UserId}", userId);
                    return NotFound(ApiResponse<string>.Fail("No settings found."));
                }

                return Ok(ApiResponse<bool[][]>.Ok(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request for User ID: {UserId}", userId);
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpPatch("settings")]
        [Authorize(Roles = "Admin, Owner")]
        public async Task<IActionResult> UpdateSwitch([FromBody] SwitchUpdateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("User ID is missing in the token.");
                return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));
            }

            try
            {
                var validationResult = await _tokenService.ValidateAdminAccessAsync(Request, User);

                if (validationResult != TokenValidationResult.Valid)
                {
                    _logger.LogWarning("Token validation failed: {ValidationResult}", validationResult);
                    return validationResult switch
                    {
                        TokenValidationResult.MissingToken => Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                        TokenValidationResult.InactiveToken => Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                        TokenValidationResult.InvalidUserId => Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                        TokenValidationResult.NoAdminPermissions =>
                            Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                        _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occurred.")) { StatusCode = 500 }
                    };
                }

                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    _logger.LogError("Failed to parse User ID: {UserId}", userId);
                    return BadRequest(ApiResponse<string>.Fail("Invalid User ID format."));
                }

                var result = await _adminSettingsService.UpdateSwitchAsync(parsedUserId, request.SectionId, request.SwitchLabel, request.NewValue);

                if (result)
                {
                    return Ok(ApiResponse<string>.Ok("Switch updated successfully."));
                }
                return BadRequest(ApiResponse<string>.Fail("Failed to update switch."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request for User ID: {UserId}", userId);
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

    }
}
