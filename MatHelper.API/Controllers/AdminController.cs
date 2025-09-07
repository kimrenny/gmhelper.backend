using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MatHelper.API.Common;

namespace MatHelper.API.Controllers
{
    [Authorize(Roles = "Admin, Owner")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IAdminSettingsService _adminSettingsService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AdminController(IAdminService adminService, IAdminSettingsService AdminSettingsService, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _adminService = adminService;
            _adminSettingsService = AdminSettingsService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

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

        [HttpPut("users/{id}/action")]
        public async Task<IActionResult> ActionUser(string id, [FromBody] AdminActionDto adminActionDto)
        {
            if(adminActionDto == null || string.IsNullOrWhiteSpace(adminActionDto.Action))
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));
            }

            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                await _adminService.ActionUserAsync(Guid.Parse(id), adminActionDto.Action);
                return Ok(ApiResponse<string>.Ok("Action performed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("tokens")]
        public async Task<IActionResult> GetTokens()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

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

        [HttpPut("tokens/{id}/action")]
        public async Task<IActionResult> ActionToken(string id, [FromBody] AdminActionDto adminActionDto)
        {
            if (adminActionDto == null || string.IsNullOrWhiteSpace(adminActionDto.Action))
            {
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));
            }

            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                await _adminService.ActionTokenAsync(id, adminActionDto.Action);
                return Ok(ApiResponse<string>.Ok("Token action performed successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("dashboard/registrations")]
        public async Task<IActionResult> GetRegistrations()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

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
        public async Task<IActionResult> GetDashboardTokens()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var activeUsers = await _adminService.GetDashboardTokensAsync();

                return Ok(ApiResponse<DashboardTokensDto>.Ok(activeUsers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("stats/country")]
        public async Task<IActionResult> GetUsersByCountry()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

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


        [HttpGet("stats/roles")]
        public async Task<IActionResult> GetRoleStats()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

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

        [HttpGet("stats/blocked")]
        public async Task<IActionResult> GetBlockStats()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

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
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

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
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    _logger.LogError("Failed to parse User ID: {UserId}", userId);
                    return BadRequest(ApiResponse<string>.Fail("Invalid User ID format."));
                }

                var result = await _adminSettingsService.UpdateSwitchAsync(parsedUserId, request.SectionTitle, request.SwitchLabel, request.NewValue);

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
