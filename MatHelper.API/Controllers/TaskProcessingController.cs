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
        private readonly ITaskProcessingService _taskProcessingService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<TaskProcessingController> _logger;

        public TaskProcessingController(ITaskProcessingService taskProcessingService, ITokenService tokenService, ILogger<TaskProcessingController> logger)
        {
            _taskProcessingService = taskProcessingService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("process")]
        public async Task<IActionResult> ProcessTask([FromBody] JsonElement taskData)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ip))
            {
                _logger.LogWarning("Failed to determine IP address.");
                return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));
            }

            var token = Request.Headers["Authorization"].ToString().Split(" ").Last();
            var userId = User.Identity?.Name;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("Processing task from IP: {Ip}, UserId: Anonymous", ip);
            }
            else
            {
                _logger.LogInformation("Processing task from IP: {Ip}, UserId: {UserId}", ip, userId);
            }

            var (allowed, retryAfter) = await _taskProcessingService.CanProcessRequestAsync(ip, userId);

            if (!allowed)
            {
                var minutes = (int)Math.Ceiling(retryAfter?.TotalMinutes ?? 0);
                return BadRequest(ApiResponse<string>.Fail($"Try again after {minutes} minutes."));
            }

            var taskId = await _taskProcessingService.ProcessTaskAsync(taskData, ip, userId);

            _logger.LogInformation("Task processed successfully. TaskId: {TaskId}", taskId);

            return Ok(ApiResponse<string>.Ok(taskId));
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetTask(string id)
        {
            try
            {
                var taskJson = await _taskProcessingService.GetTaskAsync(id);
                return Ok(ApiResponse<JsonElement>.Ok(taskJson));
            }
            catch (FileNotFoundException)
            {
                return NotFound(ApiResponse<JsonElement>.Fail("Task not found."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, ApiResponse<JsonElement>.Fail("An error occured while reading the task."));
            }
        }

        [Authorize]
        [HttpPost("rate")]
        public async Task<IActionResult> RateTask([FromBody] TaskRatingDto ratingDto)
        {
            if (string.IsNullOrEmpty(ratingDto.TaskId))
                return BadRequest(ApiResponse<string>.Fail("TaskId is required."));

            var token = Request.Headers["Authorization"].ToString().Split(" ").Last();

            if (string.IsNullOrEmpty(token))
                return Unauthorized(ApiResponse<string>.Fail("User is not authorized."));

            if (await _tokenService.IsTokenDisabled(token))
                return Unauthorized(ApiResponse<string>.Fail("User token is not active."));

            var userId = User.Identity?.Name;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID is missing in the token.");
                return Unauthorized(ApiResponse<string>.Fail("User is not authenticated."));
            }

            var taskOwnerId = await _taskProcessingService.GetTaskCreatorUserIdAsync(ratingDto.TaskId);

            if (!string.IsNullOrEmpty(taskOwnerId))
            {
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(ApiResponse<string>.Fail("Only the task creator can rate this task."));

                if (userId != taskOwnerId)
                    return Unauthorized(ApiResponse<string>.Fail("Only the task creator can rate this task."));
            }

            await _taskProcessingService.RateTaskAsync(ratingDto.TaskId, ratingDto.IsCorrect, userId);
            return Ok(ApiResponse<string>.Ok("Rating saved."));
        }
    }
}
