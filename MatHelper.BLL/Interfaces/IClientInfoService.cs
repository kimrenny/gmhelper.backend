using Microsoft.AspNetCore.Http;

namespace MatHelper.BLL.Interfaces
{
    public interface IClientInfoService
    {
        string? GetClientIp(HttpContext context);
    }
}