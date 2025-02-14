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

                var stats = await _logService.GetRequestStats();
                if(stats == null)
                {
                    return NotFound("No data available");
                }
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, "Internal server error.");
            }
        }

        
    }
}
