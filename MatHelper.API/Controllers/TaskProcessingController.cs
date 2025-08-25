using MatHelper.API.Common;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskProcessingController : ControllerBase
    {
        private readonly IGeoTaskProcessingService _geoTaskProcessingService;
        private readonly IMathTaskProcessingService _mathTaskProccessingService;
        private readonly ITokenService _tokenService;
        private readonly IClientInfoService _clientInfoService;
        private readonly ILogger<TaskProcessingController> _logger;

        public TaskProcessingController(IGeoTaskProcessingService geoTaskProcessingService, IMathTaskProcessingService mathTaskProcessingService, ITokenService tokenService, IClientInfoService clientInfoService, ILogger<TaskProcessingController> logger)
        {
            _geoTaskProcessingService = geoTaskProcessingService;
            _mathTaskProccessingService = mathTaskProcessingService;
            _tokenService = tokenService;
            _clientInfoService = clientInfoService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("geo/process")]
        public async Task<IActionResult> ProcessGeoTask([FromBody] JsonElement taskData)
        {
            try
            {
                var ip = _clientInfoService.GetClientIp(HttpContext);

                if (string.IsNullOrEmpty(ip))
                {
                    _logger.LogWarning("Failed to determine IP address.");
                    return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));
                }

                var token = _tokenService.ExtractTokenAsync(Request);
                Guid? userId = null;

                if(!string.IsNullOrEmpty(token))
                {
                    userId = await _tokenService.GetUserIdFromTokenAsync(token);

                    if (userId == null || await _tokenService.IsTokenDisabled(token))
                    {
                        _logger.LogInformation("Processing task from IP: {Ip}, UserId: Anonymous", ip);
                    }
                    else
                    {
                        _logger.LogInformation("Processing task from IP: {Ip}, UserId: {UserId}", ip, userId);
                    }
                }
                else
                {
                    _logger.LogInformation("Processing task from IP: {Ip}, UserId: {UserId}", ip, userId);
                }

                var (allowed, retryAfter) = await _geoTaskProcessingService.CanProcessRequestAsync(ip, userId);

                if (!allowed)
                {
                    var minutes = (int)Math.Ceiling(retryAfter?.TotalMinutes ?? 0);
                    return BadRequest(ApiResponse<string>.Fail($"Try again after {minutes} minutes."));
                }

                var taskId = await _geoTaskProcessingService.ProcessTaskAsync(taskData, ip, userId);

                _logger.LogInformation("Task processed successfully. TaskId: {TaskId}", taskId);

                return Ok(ApiResponse<string>.Ok(taskId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("An error occured while processing the task."));
            }
        }

        [AllowAnonymous]
        [HttpPost("math/process")]
        public async Task<IActionResult> ProcessMathTask([FromBody] JsonElement taskData)
        {
            try
            {
                var ip = _clientInfoService.GetClientIp(HttpContext);

                if (string.IsNullOrEmpty(ip))
                {
                    _logger.LogWarning("Failed to determine IP address.");
                    return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));
                }

                var token = _tokenService.ExtractTokenAsync(Request);
                Guid? userId = null;

                if (!string.IsNullOrEmpty(token))
                {
                    userId = await _tokenService.GetUserIdFromTokenAsync(token);

                    if (userId == null || await _tokenService.IsTokenDisabled(token))
                    {
                        _logger.LogInformation("Processing task from IP: {Ip}, UserId: Anonymous", ip);
                    }
                    else
                    {
                        _logger.LogInformation("Processing task from IP: {Ip}, UserId: {UserId}", ip, userId);
                    }
                }
                else
                {
                    _logger.LogInformation("Processing task from IP: {Ip}, UserId: {UserId}", ip, userId);
                }

                var (allowed, retryAfter) = await _mathTaskProccessingService.CanProcessRequestAsync(ip, userId);

                if (!allowed)
                {
                    var minutes = (int)Math.Ceiling(retryAfter?.TotalMinutes ?? 0);
                    return BadRequest(ApiResponse<string>.Fail($"Try again after {minutes} minutes."));
                }

                var taskId = await _mathTaskProccessingService.ProcessTaskAsync(taskData, ip, userId);

                _logger.LogInformation("Task processed successfully. TaskId: {TaskId}", taskId);

                return Ok(ApiResponse<string>.Ok(taskId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("An error occured while processing the task."));
            }

        }

        [HttpGet("geo/get/{id}")]
        public async Task<IActionResult> GetGeoTask(string id)
        {
            try
            {
                var taskJson = await _geoTaskProcessingService.GetTaskAsync(id);
                return Ok(ApiResponse<JsonElement>.Ok(taskJson));
            }
            catch (FileNotFoundException)
            {
                return NotFound(ApiResponse<string>.Fail("Task not found."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("An error occured while reading the task."));
            }
        }

        [HttpGet("math/get/{id}")]
        public async Task<IActionResult> GetMathTask(string id)
        {
            try
            {
                var taskJson = await _mathTaskProccessingService.GetTaskAsync(id);
                return Ok(ApiResponse<JsonElement>.Ok(taskJson));
            }
            catch (FileNotFoundException)
            {
                return NotFound(ApiResponse<string>.Fail("Task not found."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("An error occured while reading the task."));
            }
        }

        [Authorize]
        [HttpPost("geo/rate")]
        public async Task<IActionResult> RateGeoTask([FromBody] TaskRatingDto ratingDto)
        {
            try
            {
                if (string.IsNullOrEmpty(ratingDto.TaskId))
                    return BadRequest(ApiResponse<string>.Fail("TaskId is required."));

                var token = _tokenService.ExtractTokenAsync(Request);

                if (token == null || await _tokenService.IsTokenDisabled(token))
                {
                    return Unauthorized(ApiResponse<string>.Fail("User is not autorized."));
                }

                if (await _tokenService.IsTokenDisabled(token))
                    return Unauthorized(ApiResponse<string>.Fail("User token is not active."));

                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User is not authenticated."));

                var taskOwnerId = await _geoTaskProcessingService.GetTaskCreatorUserIdAsync(ratingDto.TaskId);

                if (!string.IsNullOrEmpty(taskOwnerId))
                {
                    if (userId.ToString() != taskOwnerId)
                        return Unauthorized(ApiResponse<string>.Fail("Only the task creator can rate this task."));
                }

                await _geoTaskProcessingService.RateTaskAsync(ratingDto.TaskId, ratingDto.IsCorrect, userId);
                return Ok(ApiResponse<string>.Ok("Rating saved."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("An error occured while rating the task."));
            }
        }

        [Authorize]
        [HttpPost("math/rate")]
        public async Task<IActionResult> RateMathTask([FromBody] TaskRatingDto ratingDto)
        {
            try
            {
                if (string.IsNullOrEmpty(ratingDto.TaskId))
                    return BadRequest(ApiResponse<string>.Fail("TaskId is required."));

                var token = _tokenService.ExtractTokenAsync(Request);

                if (token == null || await _tokenService.IsTokenDisabled(token))
                {
                    return Unauthorized(ApiResponse<string>.Fail("User is not autorized."));
                }

                if (await _tokenService.IsTokenDisabled(token))
                    return Unauthorized(ApiResponse<string>.Fail("User token is not active."));

                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User is not authenticated."));

                var taskOwnerId = await _mathTaskProccessingService.GetTaskCreatorUserIdAsync(ratingDto.TaskId);

                if (!string.IsNullOrEmpty(taskOwnerId))
                {
                    if (userId.ToString() != taskOwnerId)
                        return Unauthorized(ApiResponse<string>.Fail("Only the task creator can rate this task."));
                }

                await _geoTaskProcessingService.RateTaskAsync(ratingDto.TaskId, ratingDto.IsCorrect, userId);
                return Ok(ApiResponse<string>.Ok("Rating saved."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("An error occured while rating the task."));
            }
        }
    }
}
