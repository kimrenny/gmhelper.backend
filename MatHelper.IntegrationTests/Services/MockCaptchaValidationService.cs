using MatHelper.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatHelper.IntegrationTests.Services
{
    public class MockCaptchaValidationService : ICaptchaValidationService
    {
        public Task<bool> ValidateCaptchaAsync(string captchaToken)
        {
            return Task.FromResult(true);
        }
    }
}
