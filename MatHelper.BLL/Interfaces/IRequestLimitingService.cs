using Microsoft.AspNetCore.Http;

namespace MatHelper.BLL.Interfaces;

public interface IRequestLimitingService
{
    bool TryAcquire(HttpContext context, out string errorMessage);

    void Release(HttpContext context);
}