namespace MatHelper.BLL.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);

        Task<int> GetVersionAsync(string key);
        Task IncrementVersionAsync(string key);
    }
}