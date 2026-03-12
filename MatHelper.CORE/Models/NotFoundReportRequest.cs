namespace MatHelper.CORE.Models
{
    public class NotFoundReportRequest
    {
        public string Report { get; set; } = string.Empty;
        public ClientInfoDto? ClientInfo { get; set; }
    }
}