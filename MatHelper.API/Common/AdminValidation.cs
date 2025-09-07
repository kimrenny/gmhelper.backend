using MatHelper.BLL.Interfaces;
using MatHelper.BLL.Services;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using Microsoft.AspNetCore.Mvc;

namespace MatHelper.API.Common
{
    public static class AdminValidation
    {
        public static async Task<IActionResult?> ValidateAdminAsync(ControllerBase controller, ITokenService tokenService)
        {
            var validationResult = await tokenService.ValidateAdminAccessAsync(controller.Request, controller.User);
            if (validationResult == TokenValidationResult.Valid) return null;

            return validationResult switch
            {
                TokenValidationResult.MissingToken => controller.Unauthorized(ApiResponse<string>.Fail("Authorization header is missing or invalid")),
                TokenValidationResult.InactiveToken => controller.Unauthorized(ApiResponse<string>.Fail("User token is not active.")),
                TokenValidationResult.InvalidUserId => controller.Unauthorized(ApiResponse<string>.Fail("User ID is not available in the token.")),
                TokenValidationResult.NoAdminPermissions => controller.Forbid(ApiResponse<string>.Fail("User does not have permissions.").Message ?? "No admin permissions."),
                _ => new ObjectResult(ApiResponse<string>.Fail("Unexpected error occured.")) { StatusCode = 500 }
            };
        }
    }

}
