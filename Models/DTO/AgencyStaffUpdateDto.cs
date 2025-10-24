using System.ComponentModel.DataAnnotations;

namespace HotelAPI.Models.DTO
{
    public class AgencyStaffUpdateDto
    {
        [Required]
        [MaxLength(50)]
        public required string Role { get; set; }

        [MaxLength(100)]
        public string? Designation { get; set; } 

        [Required]
        [MaxLength(120)]
        public required string Name { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }
    }
}
