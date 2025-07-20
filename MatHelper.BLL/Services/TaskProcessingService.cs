using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using MatHelper.DAL.Repositories;
using System.Net;
using System.Text.Json;

namespace MatHelper.BLL.Services
{
    public class TaskProcessingService : ITaskProcessingService
    {
        private readonly TaskRequestRepository _taskRequestRepository;

        public TaskProcessingService(TaskRequestRepository taskRequestRepository)
        {
            _taskRequestRepository = taskRequestRepository;
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
                Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, $"{taskId}.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(taskData));

            var log = new TaskRequestLog
            {
                IpAddress = ip,
                RequestTime = DateTime.UtcNow,
                UserId = userId
            };

            await _taskRequestRepository.AddRequestAsync(log);
            await _taskRequestRepository.SaveChangesAsync();

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
    }
}