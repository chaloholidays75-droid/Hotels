using System.Collections.Generic;

namespace HotelAPI.Models
{
    public class HotelInfo
    {
        public int Id { get; set; }

        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "NA";
        public string City { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string HotelContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SpecialRemarks { get; set; } = string.Empty;

        
        

    }
}
