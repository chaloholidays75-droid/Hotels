using System;
using System.Collections.Generic;

namespace HotelAPI.Models
{
    public class HotelInfo : AuditableEntity
    {
        public int Id { get; set; }

        // Relations
        public int CityId { get; set; }
        public City City { get; set; } = null!;

        public int CountryId { get; set; }
        public Country Country { get; set; } = null!;
        public string? Area { get; set; }

        // Hotel Details
        public string HotelName { get; set; } = string.Empty;
        public string? HotelEmail { get; set; } // optional
        public string? HotelContactNumber { get; set; } // optional
        public string HotelChain { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SpecialRemarks { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;



        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation: Contacts / Staff
        public ICollection<HotelStaff> HotelStaff { get; set; } = new List<HotelStaff>();
        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    }
}
