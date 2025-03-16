namespace MatHelper.CORE.Models
{
    public class CombinedRequestLogDto
    {
        public List<RequestLogDto>? Regular { get; set; }
        public List<RequestLogDto>? Admin { get; set; }
    }
}
