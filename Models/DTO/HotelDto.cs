using System.Collections.Generic;

namespace HotelAPI.Models.DTO
{
    public class HotelDto
    {
        public int Id { get; set; }
        public string Country { get; set; } = "";
        public string CountryCode { get; set; } = "";
        public string City { get; set; } = "";
        public string HotelName { get; set; } = "";
        public string HotelEmail { get; set; } = "";
        public string HotelContactNumber { get; set; } = "";
        public string HotelChain { get; set; } = "";
        public string Address { get; set; } = "";
        public string SpecialRemarks { get; set; } = "";

        public List<HotelStaffDto> SalesPersons { get; set; } = new();
        public List<HotelStaffDto> ReservationPersons { get; set; } = new();
        public List<HotelStaffDto> AccountsPersons { get; set; } = new();
        public List<HotelStaffDto> ReceptionPersons { get; set; } = new();
        public List<HotelStaffDto> Concierges { get; set; } = new();
    }
}
