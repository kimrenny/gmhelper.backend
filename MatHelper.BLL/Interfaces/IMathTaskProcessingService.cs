using System.Text.Json;

namespace MatHelper.BLL.Interfaces
{
    public interface IMathTaskProcessingService
    {
        Task<(bool Allowed, TimeSpan? RetryAfter)> CanProcessRequestAsync(string ip, string? userId);
        Task<string> ProcessTaskAsync(JsonElement taskData, string ip, string? userId);
        Task<JsonElement> GetTaskAsync(string id);
        Task RateTaskAsync(string taskId, bool isCorrect, string? userId);
        Task<string?> GetTaskCreatorUserIdAsync(string taskId);
    }
}