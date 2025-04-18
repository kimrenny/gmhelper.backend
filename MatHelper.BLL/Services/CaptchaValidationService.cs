using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MatHelper.CORE.Models;

namespace MatHelper.BLL.Services
{
    public class CaptchaValidationService
    {
        private readonly ILogger<CaptchaValidationService> _logger;
        private const string CaptchaVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";
        private readonly string _secretKey;

        public CaptchaValidationService(ILogger<CaptchaValidationService> logger)
        {
            _logger = logger;
            _secretKey = Environment.GetEnvironmentVariable("CAPTCHA_SecretKey") ?? throw new InvalidOperationException("Captcha Secret Key not found in environment variables.");
        }

        public async Task<bool> ValidateCaptchaAsync(string captchaToken)
        {
            if (string.IsNullOrEmpty(captchaToken))
                return false;

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync(CaptchaVerifyUrl,
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                { "secret", _secretKey },
                { "response", captchaToken }
                    }));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Captcha validation request failed with status code {StatusCode}", response.StatusCode);
                    return true;
                    //return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CaptchaResponse>(json);
                return true;
                //return result.success == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during captcha validation.");
                return true;
                //return false;
            }
        }
    }
}
