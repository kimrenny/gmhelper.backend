using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MatHelper.BLL.Interfaces;
using MatHelper.API.Common;

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskProcessingController : ControllerBase
    {
        private readonly ITaskProcessingService _taskProcessingService;

        public TaskProcessingController(ITaskProcessingService taskProcessingService)
        {
            _taskProcessingService = taskProcessingService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessTask([FromBody] JsonElement taskData)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ip))
                return BadRequest(ApiResponse<string>.Fail("Unable to determine IP address."));

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            var (allowed, retryAfter) = await _taskProcessingService.CanProcessRequestAsync(ip, userId);

            if (!allowed)
            {
                var minutes = (int)Math.Ceiling(retryAfter?.TotalMinutes ?? 0);
                return BadRequest(ApiResponse<string>.Fail($"Try again after {minutes} minutes."));
            }

            var taskId = await _taskProcessingService.ProcessTaskAsync(taskData, ip, userId);
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
                return StatusCode(500, ApiResponse<JsonElement>.Fail("An error occured while reading the task."));
            }
        }
    }
}
