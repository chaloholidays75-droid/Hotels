using System.Collections.Generic;

namespace HotelAPI.Models
{
    public class HotelSale
    {
        public int Id { get; set; }

        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string HotelCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string HotelChain { get; set; } = string.Empty;

        // Contact Persons
        public string SalesPersonName { get; set; } = string.Empty;
        public string SalesPersonEmail { get; set; } = string.Empty;
        public string SalesPersonContact { get; set; } = string.Empty;

        public string ReservationPersonName { get; set; } = string.Empty;
        public string ReservationPersonEmail { get; set; } = string.Empty;
        public string ReservationPersonContact { get; set; } = string.Empty;

        public string AccountsPersonName { get; set; } = string.Empty;
        public string AccountsPersonEmail { get; set; } = string.Empty;
        public string AccountsPersonContact { get; set; } = string.Empty;

        public string ReceptionPersonName { get; set; } = string.Empty;
        public string ReceptionPersonEmail { get; set; } = string.Empty;
        public string ReceptionPersonContact { get; set; } = string.Empty;

        public string ConciergeName { get; set; } = string.Empty;
        public string ConciergeEmail { get; set; } = string.Empty;
        public string ConciergeContact { get; set; } = string.Empty;

        // Credit
        public string CreditCategory { get; set; } = string.Empty;

        // Facilities (store as JSON in PostgreSQL)
        public List<string> FacilitiesAvailable { get; set; } = new List<string>();
    }
}
