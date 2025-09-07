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
    [Route("api/v1/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly IGeoTaskProcessingService _geoService;
        private readonly IMathTaskProcessingService _mathService;
        private readonly ITokenService _tokenService;
        private readonly IClientInfoService _clientInfoService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(IGeoTaskProcessingService geoService, IMathTaskProcessingService mathService, ITokenService tokenService, IClientInfoService clientInfoService, ILogger<TasksController> logger)
        {
            _geoService = geoService;
            _mathService = mathService;
            _tokenService = tokenService;
            _clientInfoService = clientInfoService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("{taskType}")]
        public async Task<IActionResult> CreateTask(string taskType, [FromBody] JsonElement taskData)
        {
            try
            {
                var ip = _clientInfoService.GetClientIp(HttpContext);
                if (string.IsNullOrEmpty(ip))
                    return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));

                var token = _tokenService.ExtractTokenAsync(Request);
                Guid? userId = null;

                if(!string.IsNullOrEmpty(token))
                {
                    userId = await _tokenService.GetUserIdFromTokenAsync(token);
                    var userLog = userId == null ? "Anonymous" : userId.ToString();
                    _logger.LogInformation("Processing {TaskType} task from IP: {Ip}, UserId: {UserId}", taskType, ip, userLog);
                }

                var (allowed, retryAfter) = taskType.ToLower() switch
                {
                    "geo" => await _geoService.CanProcessRequestAsync(ip, userId),
                    "math" => await _mathService.CanProcessRequestAsync(ip, userId),
                    _ => throw new ArgumentException("Invalid task type")
                };

                if (!allowed)
                {
                    var minutes = (int)Math.Ceiling(retryAfter?.TotalMinutes ?? 0);
                    return BadRequest(ApiResponse<string>.Fail($"Try again after {minutes} minutes."));
                }

                var taskId = taskType.ToLower() switch
                {
                    "geo" => await _geoService.ProcessTaskAsync(taskData, ip, userId),
                    "math" => await _mathService.ProcessTaskAsync(taskData, ip, userId),
                    _ => throw new ArgumentException("Invalid task type")
                };

                _logger.LogInformation("Task processed successfully. TaskId: {TaskId}", taskId);

                return Ok(ApiResponse<string>.Ok(taskId));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("An error occured while processing the task."));
            }
        }

        [HttpGet("{taskType}/{id}")]
        public async Task<IActionResult> GetTask(string taskType, string id)
        {
            try
            {
                _logger.LogInformation("Get [TaskType} task request: {TaskId}", id);

                var taskJson = taskType.ToLower() switch
                {
                    "geo" => await _geoService.GetTaskAsync(id),
                    "math" => await _mathService.GetTaskAsync(id),
                    _ => throw new ArgumentException("Invalid task type")
                };

                return Ok(ApiResponse<JsonElement>.Ok(taskJson));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
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
        [HttpPost("{taskType}/{id}/rating")]
        public async Task<IActionResult> RateGeoTask(string taskType, string id, [FromBody] TaskRatingDto ratingDto)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return BadRequest(ApiResponse<string>.Fail("TaskId is required."));

                var token = _tokenService.ExtractTokenAsync(Request);

                if (string.IsNullOrEmpty(token) || await _tokenService.IsTokenDisabled(token))
                    return Unauthorized(ApiResponse<string>.Fail("User is not autorized."));

                var userId = await _tokenService.GetUserIdFromTokenAsync(token);
                if (userId == null) 
                    return Unauthorized(ApiResponse<string>.Fail("User is not authenticated."));

                string? taskOwnerId = taskType.ToLower() switch
                {
                    "geo" => await _geoService.GetTaskCreatorUserIdAsync(id),
                    "math" => await _mathService.GetTaskCreatorUserIdAsync(id),
                    _ => throw new ArgumentException("Invalid task type")
                };

                if (!string.IsNullOrEmpty(taskOwnerId) && userId.ToString() != taskOwnerId)
                {
                    return Unauthorized(ApiResponse<string>.Fail("Only the task creator can rate this task."));
                }

                switch (taskType.ToLower())
                {
                    case "geo":
                        await _geoService.RateTaskAsync(id, ratingDto.IsCorrect, userId);
                        break;
                    case "math":
                        await _mathService.RateTaskAsync(id, ratingDto.IsCorrect, userId);
                        break;
                    default:
                        throw new ArgumentException("Invalid task type");
                }

                _logger.LogInformation("{TaskType} task rated successfully. TaskId: {TaskId}, UserId: {UserId}", taskType, id, userId);
                return Ok(ApiResponse<string>.Ok("Rating saved."));
            }
            catch(ArgumentException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<string>.Fail("An error occured while rating the task."));
            }
        }
    }
}
