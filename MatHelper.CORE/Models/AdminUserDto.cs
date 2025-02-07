namespace MatHelper.CORE.Models
{
    public class AdminUserDto
    {
        public required Guid Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Role {  get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsBlocked { get; set; }
        public List<LoginTokenDto>? LoginTokens { get; set; }
    }
}