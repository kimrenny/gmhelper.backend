using MatHelper.BLL.Interfaces;
using MatHelper.DAL.Interfaces;
using MatHelper.DAL.Models;

namespace MatHelper.BLL.Services
{
    public class LoginAttemptService : ILoginAttemptService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan BlockDuration = TimeSpan.FromHours(1);

        private readonly IIpLoginAttemptRepository _repository;

        public LoginAttemptService(IIpLoginAttemptRepository repository)
        {
            _repository = repository;
        }

        public async Task CheckIpBlockedAsync(string ipAddress)
        {
            var attempt = await _repository.GetByIpAsync(ipAddress);
            if (attempt == null)
                return;

            if (attempt.BlockedUntil.HasValue && attempt.BlockedUntil > DateTime.UtcNow)
                throw new UnauthorizedAccessException("IP temporarily blocked due to multiple failed login attempts.");

            if (attempt.BlockedUntil.HasValue && attempt.BlockedUntil <= DateTime.UtcNow)
            {
                attempt.BlockedUntil = null;
                attempt.FailedCount = 0;
                await _repository.SaveChangesAsync();
            }
        }

        public async Task RegisterFailedAttemptAsync(string ipAddress)
        {
            var attempt = await _repository.GetByIpAsync(ipAddress);

            if (attempt == null)
            {
                attempt = new IpLoginAttempt
                {
                    IpAddress = ipAddress,
                    FailedCount = 1,
                    LastAttemptAt = DateTime.UtcNow
                };

                await _repository.AddAsync(attempt);
                await _repository.SaveChangesAsync();
                return;
            }

            attempt.FailedCount++;
            attempt.LastAttemptAt = DateTime.UtcNow;

            if (attempt.FailedCount >= MaxFailedAttempts)
            {
                attempt.BlockedUntil = DateTime.UtcNow.Add(BlockDuration);
                attempt.FailedCount = 0;
            }

            await _repository.SaveChangesAsync();
        }

        public async Task ResetAttemptsAsync(string ipAddress)
        {
            var attempt = await _repository.GetByIpAsync(ipAddress);
            if (attempt == null)
                return;

            attempt.FailedCount = 0;
            attempt.BlockedUntil = null;
            await _repository.SaveChangesAsync();
        }
    }
}