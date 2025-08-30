using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Http;

namespace MatHelper.BLL.Interfaces
{
    public interface IClientInfoService
    {
        string? GetClientIp(HttpContext context);
        DeviceInfo GetDeviceInfo(HttpContext context);
        (DeviceInfo, string?) GetRequestInfo(HttpContext context);
    }
}