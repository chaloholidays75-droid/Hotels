using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelAPI.Models;

namespace HotelAPI.Models
{
    public class AgencyStaff : AuditableEntity
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Agency")]
        public int AgencyId { get; set; }
        public Agency? Agency { get; set; }

        [Required]
        [MaxLength(50)]
        public string? Role { get; set; } // Sales, Reservation, Accounts, etc.

        [Required]
        [MaxLength(120)]
        public string? Name { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(120)]
        public string? Designation { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }
    }
}
