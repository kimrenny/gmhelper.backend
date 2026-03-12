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

        // todo: add microservice for sending emails, and move this logic there
    }
}
