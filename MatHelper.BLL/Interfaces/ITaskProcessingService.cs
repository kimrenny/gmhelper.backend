using System.Text.Json;

namespace MatHelper.BLL.Interfaces
{
    public interface ITaskProcessingService
    {
        Task<(bool Allowed, TimeSpan? RetryAfter)> CanProcessRequestAsync(string ip, string? userId);
        Task<string> ProcessTaskAsync(JsonElement taskData, string ip, string? userId);
        Task<JsonElement> GetTaskAsync(string id);
    }
}