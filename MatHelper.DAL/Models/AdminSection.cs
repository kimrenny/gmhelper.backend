using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MatHelper.DAL.Models
{
    public class AdminSection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AdminSettingsId { get; set; }

        [ForeignKey("AdminSettingsId")]
        public required AdminSettings AdminSettings { get; set; }

        public required string Title { get; set; }

        public List<AdminSwitch> Switches { get; set; } = new();
    }
}
