namespace MatHelper.CORE.Models
{
    public class ConfirmCodeDto
    {
        public required string Code { get; set; }
        public required string SessionKey { get; set; }
    }
}