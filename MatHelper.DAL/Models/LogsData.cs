using MatHelper.CORE.Models;

namespace MatHelper.DAL.Models
{
    public class LogsData
    {
        public required CombinedRequestLogDto RequestStats {  get; set; }
        public required PagedResult<RequestLogDetail> RequestLogs { get; set; }
        public required PagedResult<AuthLog> AuthLogs { get; set; }
        public required PagedResult<ErrorLog> ErrorLogs { get; set; }
    }
}