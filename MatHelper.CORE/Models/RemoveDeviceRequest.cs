namespace MatHelper.CORE.Models
{
    public class RemoveDeviceRequest
    {
        public required string UserAgent { get; set; }
        public required string Platform { get; set; }
        public required string IpAddress { get; set; }
    }
}
