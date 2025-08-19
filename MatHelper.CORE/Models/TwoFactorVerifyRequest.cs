namespace MatHelper.CORE.Models
{
    public class TwoFactorRequest
    {
        public required string Type { get; set; }
        public required string Code { get; set; }
    }
}