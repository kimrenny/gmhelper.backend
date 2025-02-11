using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatHelper.CORE.Models
{
    public class CaptchaResponse
    {
        public bool Success { get; set; }
        public string? ChallengeTs { get; set; }
        public string? Hostname { get; set; }
        public List<string>? ErrorCodes { get; set; }
    }
}
