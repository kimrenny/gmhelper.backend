using Microsoft.AspNetCore.Mvc;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Models;
using MatHelper.CORE.Exceptions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MatHelper.API.Common;
using MatHelper.DAL.Models;

namespace MatHelper.API.Controllers
{
    [Authorize(Roles = "Owner")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class OwnerController : ControllerBase
    {
        private readonly IOwnerService _ownerService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<OwnerController> _logger;

        public OwnerController(IOwnerService ownerService, ITokenService tokenService, ILogger<OwnerController> logger)
        {
            _ownerService = ownerService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeUserRoleRequest request)
        {
            try
            {
                var ownerValidation = await AdminValidation.ValidateAdminAsync(this, _tokenService);
                if (ownerValidation != null) return ownerValidation;

                var authToken = _tokenService.ExtractTokenAsync(Request);
                if(authToken == null)
                    return Unauthorized(ApiResponse<string>.Fail("Unauthorized."));

                var requesterUserId = await _tokenService.GetUserIdFromTokenAsync(authToken);
                if (requesterUserId == null)
                    return Unauthorized(ApiResponse<string>.Fail("Unauthorized."));

                await _ownerService.ChangeUserRoleAsync(requesterUserId.Value, userId, request.Role);

                return Ok(ApiResponse<string>.Ok("User role updated successfully."));
            }
            catch(TwoFactorRequiredException ex)
            {
                return new ObjectResult(ApiResponse<string>.Fail(ex.Message))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change user role. UserId: {UserId}", userId);
                return new ObjectResult(ApiResponse<string>.Fail("Internal server error.")) { StatusCode = 500 };
            }
        }
    }
}
