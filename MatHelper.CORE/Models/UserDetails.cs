namespace MatHelper.CORE.Models
{
    public class UserDetails
    {
        public byte[]? Avatar { get; set; }
        public required string Nickname { get; set; }
        public required string Language { get; set; } = "EN";
    }
}