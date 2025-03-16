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
using MatHelper.DAL.Models;

namespace MatHelper.API.Controllers
{
    [Authorize(Roles = "Admin, Owner")]
    [ApiController]
    [Route("api/admin/[controller]")]
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

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequestStats()
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

                var stats = await _logService.GetRequestStats();
                if(stats == null)
                {
                    return NotFound(ApiResponse<string>.Fail("No data available"));
                }
                return Ok(ApiResponse<CombinedRequestLogDto>.Ok(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllRequests()
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

        [HttpGet("auth/all")]
        public async Task<IActionResult> GetAllAuthLogs()
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

        [HttpGet("errors/all")]
        public async Task<IActionResult> GetAllErrorLogs()
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
