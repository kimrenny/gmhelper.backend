using MatHelper.CORE.Models;

namespace MatHelper.DAL.Models
{
    public class AdminDataDto
    {
        public required PagedResult<AdminUserDto> Users { get; set; }
        public required PagedResult<TokenDto> Tokens { get; set; }
        public required List<RegistrationsDto> Registrations { get; set; }
        public required DashboardTokensDto DashboardTokens { get; set; }
        public required List<CountryStatsDto> CountryStats { get; set; }
        public required List<RoleStatsDto> RoleStats { get; set; }
        public required List<BlockStatsDto> BlockStats { get; set; }
        public required CombinedRequestLogDto RequestStats { get; set; }
        public required PagedResult<RequestLogDetail> RequestLogs { get; set; }
        public required PagedResult<AuthLog> AuthLogs { get; set; }
        public required PagedResult<ErrorLog> ErrorLogs { get; set; }
    }
}