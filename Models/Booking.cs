using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelAPI.Models
{
    public class Booking : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        public string? TicketNumber { get; set; }

        // Foreign keys
        public int? AgencyId { get; set; }
        public int? SupplierId { get; set; }
        public int? HotelId { get; set; }

        // Navigation properties
        public virtual Agency? Agency { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual HotelInfo? Hotel { get; set; }

        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int? Nights { get; set; } // computed in DB

        public int? NumberOfRooms { get; set; } // Total rooms booked

        // Optional occupancy summary for booking
        public int? NumberOfPeople { get; set; }

        public string? SpecialRequest { get; set; }
        public string? Status { get; set; } = "Pending";

        // ✅ Link to Commercial
        public int? CommercialId { get; set; }

        [ForeignKey(nameof(CommercialId))]
        public Commercial? Commercial { get; set; }

        // Navigation: BookingRooms
        public virtual ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
    }
}
