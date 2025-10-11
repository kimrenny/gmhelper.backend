using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Authorization;
using MatHelper.API.Common;
using MatHelper.DAL.Models;

namespace MatHelper.API.Controllers
{
    [Authorize(Roles = "Admin, Owner")]
    [ApiController]
    [Route("api/v1/admin/[controller]")]
    public class LogsController : Controller
    {
        private readonly IRequestLogService _logService;
        private readonly ILogger<LogsController> _logger;
        private readonly ITokenService _tokenService;

        public LogsController(IRequestLogService logService, ILogger<LogsController> logger, ITokenService tokenService)
        {
            _logService = logService;
            _logger = logger;
            _tokenService = tokenService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetLogsStats()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var stats = await _logService.GetRequestStats();
                if(stats == null)
                {
                    return NotFound(ApiResponse<string>.Fail("No data available"));
                }
                return Ok(ApiResponse<CombinedRequestLogDto>.Ok(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs stats.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(int page = 1, int pageSize = 10, string sortBy = "Id", bool descending = true, DateTime? maxLogDate = null)
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var pagedLogs = await _logService.GetRequestLogs(page, pageSize, sortBy, descending, maxLogDate);
                if (pagedLogs == null || !pagedLogs.Items.Any())
                {
                    _logger.LogError("Requests data not found.");
                    return NotFound(ApiResponse<string>.Fail("Requests data not found."));
                }

                return Ok(ApiResponse<PagedResult<RequestLogDetail>>.Ok(pagedLogs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("auth")]
        public async Task<IActionResult> GetAuthLogs(int page = 1, int pageSize = 10, string sortBy = "Id", bool descending = true, DateTime? maxLogDate = null)
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var pagedLogs = await _logService.GetAuthLogs(page, pageSize, sortBy, descending, maxLogDate);
                if (pagedLogs == null || !pagedLogs.Items.Any())
                {
                    _logger.LogError("Auth logs not found.");
                    return NotFound(ApiResponse<string>.Fail("Auth logs not found."));
                }

                return Ok(ApiResponse<PagedResult<AuthLog>>.Ok(pagedLogs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("errors")]
        public async Task<IActionResult> GetErrorLogs(int page = 1, int pageSize = 10, string sortBy = "Id", bool descending = true, DateTime? maxLogDate = null)
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var pagedLogs = await _logService.GetErrorLogs(page, pageSize, sortBy, descending, maxLogDate);
                if (pagedLogs == null || !pagedLogs.Items.Any())
                {
                    _logger.LogError("Error logs not found.");
                    return NotFound(ApiResponse<string>.Fail("Error logs not found."));
                }

                return Ok(ApiResponse<PagedResult<ErrorLog>>.Ok(pagedLogs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }
    }
}
