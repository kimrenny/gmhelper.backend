using MatHelper.API.Common;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MatHelper.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecurityController : ControllerBase
    {
        private readonly ITwoFactorService _twoFactorService;
        private readonly UserRepository _userRepository;
        private readonly LoginTokenRepository _loginTokenRepository;
        private readonly ILogger<SecurityController> _logger;

        public SecurityController(ITwoFactorService twoFactorService, UserRepository userRepository, LoginTokenRepository loginTokenRepository, ILogger<SecurityController> logger)
        {
            _twoFactorService = twoFactorService;
            _userRepository = userRepository;
            _loginTokenRepository = loginTokenRepository;
            _logger = logger;
        }

        [HttpPost("2fa/generate")]
        [Authorize]
        public async Task<IActionResult> GenerateTwoFactorKey([FromBody] TwoFactorType type)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "Authorization header is missing or invalid" });

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var userId = await _loginTokenRepository.GetUserIdByAuthTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User not found"));

                var user = await _userRepository.GetUserByIdAsync(userId.Value);
                if (user == null) return Unauthorized(ApiResponse<string>.Fail("User not found"));

                var twoFactor = await _twoFactorService.GenerateTwoFAKeyAsync(userId.Value, type.Type);
                if (twoFactor == null || twoFactor.Secret == null) return BadRequest(ApiResponse<string>.Fail(""));

                var qrCode = _twoFactorService.GenerateQrCode(twoFactor.Secret, user.Email);

                var response = new TwoFactorResponse
                {
                    QrCode = qrCode,
                    Secret = twoFactor.Secret
                };

                return Ok(ApiResponse<TwoFactorResponse>.Ok(response));
            }
            catch(InvalidOperationException ex)
            {
                _logger.LogInformation(ex, "Error while generating 2fa key for user");
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating 2fa key for user");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occured."));
            }

        }

        [HttpPost("2fa/verify")]
        [Authorize]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var userId = await _loginTokenRepository.GetUserIdByAuthTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User not found"));

                var isValid = await _twoFactorService.VerifyTwoFACodeAsync(userId.Value, request.Type, request.Code);
                if (!isValid) return BadRequest(ApiResponse<string>.Fail("Invalid 2FA code"));

                return Ok(ApiResponse<string>.Ok("Two-factor authentication enabled successfully"));
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while verifying 2fa code for user");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred"));
            }
        }

        [HttpPost("2fa/change-mode")]
        [Authorize]
        public async Task<IActionResult> ChangeTwoFactorMode([FromBody] TwoFactorModeRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var userId = await _loginTokenRepository.GetUserIdByAuthTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User not found"));

                var twoFactor = await _twoFactorService.GetTwoFactorAsync(userId.Value, request.Type);
                if (twoFactor == null || !twoFactor.IsEnabled) return BadRequest(ApiResponse<string>.Fail("Two-factor authentication is not enabled"));

                var isValid = _twoFactorService.VerifyTotp(twoFactor.Secret!, request.Code);
                if (!isValid) return BadRequest(ApiResponse<string>.Fail("Invalid 2FA code"));

                await _twoFactorService.ChangeTwoFactorModeAsync(userId.Value, request.Type, request.AlwaysAsk);

                return Ok(ApiResponse<string>.Ok("Two-factor authentication mode updated successfully"));
;           }
            catch(InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error while changing 2FA mode for user");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred"));
            }
        }

        [HttpPost("2fa/remove")]
        [Authorize]
        public async Task<IActionResult> RemoveTwoFactor([FromBody] TwoFactorRequest request)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid"));

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var userId = await _loginTokenRepository.GetUserIdByAuthTokenAsync(token);
                if (userId == null) return Unauthorized(ApiResponse<string>.Fail("User not found"));

                var twoFactor = await _twoFactorService.GetTwoFactorAsync(userId.Value, request.Type);
                if (twoFactor == null || !twoFactor.IsEnabled) return BadRequest(ApiResponse<string>.Fail("Two-factor authentication is not enabled"));

                var isValid = _twoFactorService.VerifyTotp(twoFactor.Secret!, request.Code);
                if (!isValid) return BadRequest(ApiResponse<string>.Fail("Invalid 2FA code"));

                await _twoFactorService.RemoveTwoFactorAsync(twoFactor);

                return Ok(ApiResponse<string>.Ok("Two-factor authentication removed successfully"));
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error while removing 2fa for user");
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred"));
            }
        }

    }
}
