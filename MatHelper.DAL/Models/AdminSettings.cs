using MatHelper.CORE.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MatHelper.DAL.Models
{
    public class AdminSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public required User User { get; set; }

        public List<AdminSection> Sections { get; set; } = new();
    }
}
