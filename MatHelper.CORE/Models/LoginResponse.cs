using System;

namespace MatHelper.CORE.Models
{
    public class LoginResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? Message { get; set; }
        public string? SessionKey { get; set; }
    }
}