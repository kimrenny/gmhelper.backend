namespace MatHelper.CORE.Models
{
    public class AdminData
    {
        public required PagedResult<AdminUserDto> Users { get; set; }
        public required PagedResult<TokenDto> Tokens { get; set; }
        public required List<RegistrationsDto> Registrations { get; set; }
        public required DashboardTokensDto DashboardTokens { get; set; }
        public required List<CountryStatsDto> CountryStats { get; set; }
        public required List<RoleStatsDto> RoleStats { get; set; }
        public required List<BlockStatsDto> BlockStats { get; set; }
    }
}