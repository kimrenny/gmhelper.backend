using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MatHelper.BLL.Services;
using MatHelper.CORE.Models;
using MatHelper.BLL.Interfaces;
using MatHelper.DAL.Models;

namespace MatHelper.Tests.BLL
{
    public class AdminServiceTests
    {
        private readonly Mock<IUserAdminService> _userAdminServiceMock = new();
        private readonly Mock<ITokenAdminService> _tokenAdminServiceMock = new();
        private readonly Mock<IReportService> _reportServiceMock = new();
        private readonly Mock<IAnalyticsService> _analyticsServiceMock = new();
        private readonly Mock<ILogger<AdminService>> _loggerMock = new();

        private AdminService CreateService()
        {
            return new AdminService(
                _userAdminServiceMock.Object,
                _tokenAdminServiceMock.Object,
                _reportServiceMock.Object,
                _analyticsServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetAdminDataAsync_ShouldAggregateData()
        {
            var userId = Guid.NewGuid();

            var users = new PagedResult<AdminUserDto>
            {
                Items = new List<AdminUserDto>()
            };

            var tokens = new PagedResult<TokenDto>
            {
                Items = new List<TokenDto>()
            };

            var reports = new PagedResult<NotFoundReport>
            {
                Items = new List<NotFoundReport>()
            };

            _userAdminServiceMock.Setup(x =>
                x.GetUsersAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<DateTime?>()
                ))
                .ReturnsAsync(users);

            _tokenAdminServiceMock.Setup(x =>
                x.GetTokensAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<DateTime?>()
                ))
                .ReturnsAsync(tokens);

            _reportServiceMock.Setup(x =>
                x.GetNotFoundReportsAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(reports);

            _analyticsServiceMock.Setup(x => x.GetRegistrationsAsync())
                .ReturnsAsync(new List<RegistrationsDto>());

            _analyticsServiceMock.Setup(x => x.GetUsersByCountryAsync())
                .ReturnsAsync(new List<CountryStatsDto>());

            _analyticsServiceMock.Setup(x => x.GetRoleStatsAsync())
                .ReturnsAsync(new List<RoleStatsDto>());

            _analyticsServiceMock.Setup(x => x.GetBlockStatsAsync())
                .ReturnsAsync(new List<BlockStatsDto>());

            _tokenAdminServiceMock.Setup(x => x.GetDashboardTokensAsync())
                .ReturnsAsync(new DashboardTokensDto()
                {
                    ActiveTokens = 0,
                    TotalTokens = 0,
                    ActiveAdminTokens = 0,
                    TotalAdminTokens = 0
                });

            var service = CreateService();

            var result = await service.GetAdminDataAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(users, result.Users);
            Assert.Equal(tokens, result.Tokens);
            Assert.Equal(reports, result.NotFoundReports);
        }
    }
}