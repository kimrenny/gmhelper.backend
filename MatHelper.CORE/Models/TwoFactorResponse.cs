namespace MatHelper.CORE.Models
{
    public class TwoFactorResponse
    {
        public string? QrCode { get; set; }
        public string? Secret { get; set; }
    }
}