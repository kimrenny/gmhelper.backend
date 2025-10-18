using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MatHelper.API.Common;
using MatHelper.DAL.Models;

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
        private readonly IRequestLogService _requestLogService;
        private readonly ILogger<AuthController> _logger;

        public AdminController(IAdminService adminService, IAdminSettingsService AdminSettingsService, ITokenService tokenService, IRequestLogService requestLogService, ILogger<AuthController> logger)
        {
            _adminService = adminService;
            _adminSettingsService = AdminSettingsService;
            _tokenService = tokenService;
            _requestLogService = requestLogService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAdminData()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var userId = User.FindFirstValue(ClaimTypes.Name);
                if (!Guid.TryParse(userId, out var parsedUserId))
                {
                    _logger.LogError("Failed to parse User ID: {UserId}", userId);
                    return BadRequest(ApiResponse<string>.Fail("Invalid User ID format."));
                }

                var adminData = await _adminService.GetAdminDataAsync(parsedUserId);
                var adminLogs = await _requestLogService.GetLogs();

                if (adminData == null || adminLogs == null)
                {
                    return NotFound(ApiResponse<string>.Fail("Admin data or logs not found."));
                }

                var result = new AdminDataDto
                {
                    Users = adminData.Users,
                    Tokens = adminData.Tokens,
                    Registrations = adminData.Registrations,
                    DashboardTokens = adminData.DashboardTokens,
                    CountryStats = adminData.CountryStats,
                    RoleStats = adminData.RoleStats,
                    BlockStats = adminData.BlockStats,
                    RequestStats = adminLogs.RequestStats,
                    RequestLogs = adminLogs.RequestLogs,
                    AuthLogs = adminLogs.AuthLogs,
                    ErrorLogs = adminLogs.ErrorLogs
                };

                return Ok(ApiResponse<AdminDataDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all admin data.");
                return new ObjectResult(ApiResponse<string>.Fail("Internal server error.")) { StatusCode = 500 };
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 10, string sortBy = "RegistrationDate", bool descending = false, DateTime? maxRegistrationDate = null)
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var pagedUsers = await _adminService.GetUsersAsync(page, pageSize, sortBy, descending, maxRegistrationDate);
                if (pagedUsers.Items == null || !pagedUsers.Items.Any())
                {
                    _logger.LogError("Users data not found.");
                    return new ObjectResult(ApiResponse<string>.Fail("Users data not found.")) { StatusCode = 404 };
                }

                return Ok(ApiResponse<PagedResult<AdminUserDto>>.Ok(pagedUsers));
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
        public async Task<IActionResult> GetTokens(int page = 1, int pageSize = 10, string sortBy = "Expiration", bool descending = false, DateTime? maxExpirationDate = null)
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var pagedTokens = await _adminService.GetTokensAsync(page, pageSize, sortBy, descending, maxExpirationDate);
                if (pagedTokens == null || !pagedTokens.Items.Any())
                {
                    _logger.LogError("Users data not found.");
                    return NotFound(ApiResponse<string>.Fail("Users data not found."));
                }

                return Ok(ApiResponse<PagedResult<TokenDto>>.Ok(pagedTokens));
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

        [HttpPatch("settings/{section}/{label}")]
        public async Task<IActionResult> UpdateSwitch(string section, string label, [FromBody] SwitchUpdateRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token."));

            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                if (!Guid.TryParse(userId, out var parsedUserId))
                    return BadRequest(ApiResponse<string>.Fail("Invalid User ID format."));

                var result = await _adminSettingsService.UpdateSwitchAsync(parsedUserId, section, label, request.NewValue);

                if (!result)
                    return BadRequest(ApiResponse<string>.Fail("Failed to update switch."));

                return Ok(ApiResponse<string>.Ok("Switch updated successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request for User ID: {UserId}", userId);
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

    }
}
