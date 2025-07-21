using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using MatHelper.DAL.Repositories;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace MatHelper.BLL.Services
{
    public class TaskProcessingService : ITaskProcessingService
    {
        private readonly TaskRequestRepository _taskRequestRepository;
        private readonly TaskRatingRepository _taskRatingRepository;
        private readonly ILogger<TaskProcessingService> _logger;

        public TaskProcessingService(TaskRequestRepository taskRequestRepository, TaskRatingRepository taskRatingRepository, ILogger<TaskProcessingService> logger)
        {
            _taskRequestRepository = taskRequestRepository;
            _taskRatingRepository = taskRatingRepository;
            _logger = logger;
        }

        public async Task<(bool Allowed, TimeSpan? RetryAfter)> CanProcessRequestAsync(string ip, string? userId)
        {
            if (!string.IsNullOrEmpty(userId))
                return (true, null);

            var latest = await _taskRequestRepository.GetLastRequestByIpAsync(ip);

            if (latest == null)
                return (true, null);

            var now = DateTime.UtcNow;
            var diff = now - latest.RequestTime;

            if (diff.TotalHours >= 24)
                return (true, null);

            return (false, TimeSpan.FromHours(24) - diff);
        }

        public async Task<string> ProcessTaskAsync(JsonElement taskData, string ip, string? userId)
        {
            string taskId = Guid.NewGuid().ToString();
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Tasks");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                _logger.LogInformation("Created task directory at: {FolderPath}", folderPath);
            }


            string filePath = Path.Combine(folderPath, $"{taskId}.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(taskData));
            _logger.LogInformation("Saved task JSON to file: {FilePath}", filePath);

            var log = new TaskRequestLog
            {
                TaskId = taskId,
                IpAddress = ip,
                RequestTime = DateTime.UtcNow,
                UserId = userId
            };

            await _taskRequestRepository.AddRequestAsync(log);
            await _taskRequestRepository.SaveChangesAsync();

            _logger.LogInformation("Task log saved. TaskId: {TaskId}, IP: {Ip}, UserId: {UserId}", taskId, ip, userId ?? "Anonymous");

            return taskId;
        }

        public async Task<JsonElement> GetTaskAsync(string id)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Tasks", $"{id}.json");

            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            var jsonString = await File.ReadAllTextAsync(filePath);
            using var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement.Clone();
        }

        public async Task RateTaskAsync(string taskId, bool isCorrect, string? userId)
        {
            var rating = new TaskRating 
            { 
                TaskId = taskId, 
                IsCorrect = isCorrect, 
                UserId = userId, 
                CreatedAt = DateTime.UtcNow 
            };

            await _taskRatingRepository.AddRatingAsync(rating);
        }

        public async Task<string?> GetTaskCreatorUserIdAsync(string taskid)
        {
            var requestLog = await _taskRequestRepository.GetRequestByTaskIdAsync(taskid);
            return requestLog.UserId;
        }
    }
}