using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelAPI.Models
{
    public class RoomType : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HotelId { get; set; }  // Link to Hotel

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;  // e.g., Deluxe, Suite, etc.

        public bool IsActive { get; set; } = true;  // soft delete / inactive rooms

        // Navigation property
        public virtual HotelInfo? Hotel { get; set; }
    }
}
