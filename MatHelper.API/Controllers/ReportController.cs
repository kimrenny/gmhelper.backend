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
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService reportService, ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        [HttpPost("notfound")]
        public async Task<IActionResult> SubmitNotFoundReport([FromBody] NotFoundReportRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Report))
                {
                    return BadRequest(ApiResponse<string>.Fail("Report text cannot be empty."));
                }

                if (request.Report.Length > 500)
                {
                    return BadRequest(ApiResponse<string>.Fail("Report text cannot exceed 500 characters."));
                }

                await _reportService.SubmitNotFoundReportAsync(request);

                return Ok(ApiResponse<string>.Ok("Report submitted successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting not found report.");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error."));
            }
        }
    }
}
