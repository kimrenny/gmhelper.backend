using System.Text.Json;

namespace MatHelper.BLL.Interfaces
{
    public interface IGeoTaskProcessingService
    {
        Task<(bool Allowed, TimeSpan? RetryAfter)> CanProcessRequestAsync(string ip, Guid? userId);
        Task<string> ProcessTaskAsync(JsonElement taskData, string ip, Guid? userId);
        Task<JsonElement> GetTaskAsync(string id);
        Task RateTaskAsync(string taskId, bool isCorrect, Guid? userId);
        Task<string?> GetTaskCreatorUserIdAsync(string taskId);
    }
}