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
        public async Task<IActionResult> GetLogs()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var requests = await _logService.GetRequestLogs();
                if (requests == null || !requests.Any())
                {
                    _logger.LogError("Requests data not found.");
                    return NotFound(ApiResponse<string>.Fail("Requests data not found."));
                }

                return Ok(ApiResponse<List<RequestLogDetail>>.Ok(requests));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("auth")]
        public async Task<IActionResult> GetAuthLogs()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var logs = await _logService.GetAuthLogs();
                if (logs == null || !logs.Any())
                {
                    _logger.LogError("Auth logs not found.");
                    return NotFound(ApiResponse<string>.Fail("Auth logs not found."));
                }

                return Ok(ApiResponse<List<AuthLog>>.Ok(logs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("errors")]
        public async Task<IActionResult> GetErrorLogs()
        {
            try
            {
                var adminValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (adminValidation != null) return adminValidation;

                var logs = await _logService.GetErrorLogs();
                if (logs == null || !logs.Any())
                {
                    _logger.LogError("Error logs not found.");
                    return NotFound(ApiResponse<string>.Fail("Error logs not found."));
                }

                return Ok(ApiResponse<List<ErrorLog>>.Ok(logs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }
    }
}
