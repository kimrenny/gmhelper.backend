namespace MatHelper.CORE.Models
{
    public class AdminUserDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role {  get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsBlocked { get; set; }
        public List<LoginTokenDto> LoginTokens { get; set; }
    }
}