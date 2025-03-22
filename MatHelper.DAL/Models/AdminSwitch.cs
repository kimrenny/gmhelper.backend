using MatHelper.CORE.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MatHelper.DAL.Models
{
    public class AdminSwitch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AdminSectionId { get; set; }

        [ForeignKey("AdminSectionId")]
        public required AdminSection AdminSection { get; set; }

        public required string Label { get; set; }
        public bool Value { get; set; }
    }
}
