using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MatHelper.CORE.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }
        public string? Role { get; set; }
        public byte[]? Avatar { get; set; }
        public ICollection<LoginToken>? LoginTokens { get; set; }
    }
}