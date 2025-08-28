using System.Collections.Generic;

namespace HotelAPI.Models
{
    public class HotelInfo
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

        public ICollection<HotelStaff> HotelStaff { get; set; } = new List<HotelStaff>();
    }
}
