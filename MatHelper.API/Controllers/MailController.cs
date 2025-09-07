using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.OpenApi.Validations;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using MatHelper.CORE.Enums;
using MatHelper.API.Common;
using System.Diagnostics;

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MailController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<MailController> _logger;
        private readonly ICaptchaValidationService _captchaValidationService;
        private static readonly ConcurrentDictionary<string, bool> ProcessingTokens = new();

        public MailController(IAuthenticationService authenticationService, ILogger<MailController> logger, ICaptchaValidationService captchaValidationService)
        {
            _authenticationService = authenticationService;
            _logger = logger;
            _captchaValidationService = captchaValidationService;
        }

        [HttpPost("password/recover")]
        public async Task<IActionResult> SendRecoveryPasswordEmail([FromBody] PasswordRecoveryEmailDto recoveryDto)
        {
            if (!await _captchaValidationService.ValidateCaptchaAsync(recoveryDto.CaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for user: {Email}", recoveryDto.Email);
                return BadRequest(ApiResponse<string>.Fail("Invalid CAPTCHA token."));
            }

            try
            {
                var result = await _authenticationService.SendRecoverPasswordLinkAsync(recoveryDto.Email);

                if (result)
                {
                    return Ok(ApiResponse<string>.Ok("Recovery link sent."));
                }
                else
                {
                    return BadRequest(ApiResponse<string>.Fail("A problem occurred while executing the request. Please try again later."));
                }
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning($"Invalid or expired token error occured: {ex.Message}");
                return BadRequest(ApiResponse<string>.Fail("Invalid or expired token."));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Email confirmation failed: {ex.Message}");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }
        }
    }
}
