namespace MatHelper.CORE.Models
{
    public class TwoFactorModeRequest
    {
        public required string Type { get; set; }
        public required bool AlwaysAsk { get; set; }
        public required string Code { get; set; }
    }
}