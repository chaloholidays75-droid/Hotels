using System.Collections.Generic;

namespace HotelAPI.Models.DTO
{
    public class HotelDto
    {
        public int Id { get; set; }
        public int CountryId { get; set; } 
        public int CityId { get; set; } 
        public string? Region { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string HotelEmail { get; set; } = string.Empty;
        public string HotelContactNumber { get; set; } = string.Empty;
        public string HotelChain { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SpecialRemarks { get; set; } = string.Empty;

        // Staff by roles
        public List<HotelStaffDto> SalesPersons { get; set; } = new();
        public List<HotelStaffDto> ReceptionPersons { get; set; } = new();
        public List<HotelStaffDto> ReservationPersons { get; set; } = new();
        public List<HotelStaffDto> AccountsPersons { get; set; } = new();
        public List<HotelStaffDto> Concierges { get; set; } = new();

        // Active / Inactive
        public bool IsActive { get; set; } = true;
    }
}
