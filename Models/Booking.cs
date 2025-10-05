using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelAPI.Models
{
    public class Booking : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        public string? TicketNumber { get; set; }

        public int? AgencyId { get; set; }
        public int? SupplierId { get; set; }
        public int? HotelId { get; set; }

        // Navigation properties
        public Agency? Agency { get; set; }
        public Supplier? Supplier { get; set; }
        public HotelInfo? Hotel { get; set; }

        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int? Nights { get; set; } // computed in DB

        public int? NumberOfRooms { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }
        public string? ChildrenAges { get; set; }
        public string? SpecialRequest { get; set; }
        public string? Status { get; set; } = "Pending";

    }
}
