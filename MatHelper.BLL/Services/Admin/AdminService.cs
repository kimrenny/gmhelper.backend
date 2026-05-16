using Azure.Core;
using MatHelper.BLL.Interfaces;
using MatHelper.CORE.Enums;
using MatHelper.CORE.Models;
using MatHelper.CORE.Options;
using MatHelper.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace MatHelper.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserAdminService _userAdminService;
        private readonly ITokenAdminService _tokenAdminService;
        private readonly IReportService _reportService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger _logger;

        private const short DefaultPageNumber = 1;
        private const short DefaultPageSize = 10;

        public AdminService(IUserAdminService userAdminService, ITokenAdminService tokenAdminService, IReportService reportService, IAnalyticsService analyticsService, ILogger<AdminService> logger)
        {
            _userAdminService = userAdminService;
            _tokenAdminService = tokenAdminService;
            _reportService = reportService;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        public async Task<AdminData> GetAdminDataAsync(Guid userId)
        {
            var pagedUsers = await _userAdminService.GetUsersAsync(DefaultPageNumber, DefaultPageSize, "RegistrationDate", false, null);
            var pagedTokens = await _tokenAdminService.GetTokensAsync(DefaultPageNumber, DefaultPageSize, "Expiration", true, null);
            var pagedNotFoundReports = await _reportService.GetNotFoundReportsAsync(DefaultPageNumber, DefaultPageSize, "IsResolved", false);
            var registrations = await _analyticsService.GetRegistrationsAsync();
            var dashboardTokens = await _tokenAdminService.GetDashboardTokensAsync();
            var countryStats = await _analyticsService.GetUsersByCountryAsync();
            var roleStats = await _analyticsService.GetRoleStatsAsync();
            var blockStats = await _analyticsService.GetBlockStatsAsync();

            var result = new AdminData
            {
                Users = pagedUsers,
                Tokens = pagedTokens,
                NotFoundReports = pagedNotFoundReports,
                Registrations = registrations,
                DashboardTokens = dashboardTokens,
                CountryStats = countryStats,
                RoleStats = roleStats,
                BlockStats = blockStats,
            };

            return result;
        }
    }
}