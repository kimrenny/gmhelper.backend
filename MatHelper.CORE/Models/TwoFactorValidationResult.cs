namespace MatHelper.CORE.Models
{
    public class TwoFactorValidationResult
    {
        public bool Success { get; set; }
        public Guid? UserId { get; set; }
        public string? Error { get; set; }
    }
}