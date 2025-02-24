namespace MatHelper.CORE.Models
{
    public class DashboardTokensDto
    {
        public required int ActiveTokens { get; set; }
        public required int TotalTokens { get; set; }
        public required int ActiveAdminTokens { get; set; }
        public required int TotalAdminTokens { get; set; }
    }
}
