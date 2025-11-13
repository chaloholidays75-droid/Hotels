using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelAPI.Models
{
    public class BookingRoom : AuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }  // Link to Booking

        [Required]
        public int RoomTypeId { get; set; } // Always references RoomType table

  
        public int? Adults { get; set; }
        public int? Children { get; set; }
        [Column(TypeName = "jsonb")]
        public List<string>? ChildrenAges { get; set; } = new();


        public string Inclusion { get; set; } = "Room Only"; // options: Room Only / With Breakfast
        public string? LeadGuestName { get; set; }

        [Column(TypeName = "jsonb")]
        public List<string>? GuestNames { get; set; } = new();


    
        // Navigation properties
        public virtual Booking? Booking { get; set; }
        public virtual RoomType? RoomType { get; set; }
    }
}
