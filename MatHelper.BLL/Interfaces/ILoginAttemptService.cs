namespace MatHelper.BLL.Interfaces
{
    public interface ILoginAttemptService
    {
        Task CheckIpBlockedAsync(string ipAddress);
        Task RegisterFailedAttemptAsync(string ipAddress);
        Task ResetAttemptsAsync(string ipAddress);
    }
}