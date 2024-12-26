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
        private const string SecretKey = "6LdH7qIqAAAAAKR42QLAvn6TEdjKlCdmxKi85ju3";

        public CaptchaValidationService(ILogger<CaptchaValidationService> logger)
        {
            _logger = logger;
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
                { "secret", SecretKey },
                { "response", captchaToken }
                    }));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Captcha validation request failed with status code {StatusCode}", response.StatusCode);
                    return true;
                    //return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                dynamic result = JsonSerializer.Deserialize<CaptchaResponse>(json);
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
