using System;

namespace HotelAPI.Models
{
    public class Agency
    {
        public int Id { get; set; }
        public string? AgencyName { get; set; }

        // Foreign key to Country
        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        // Foreign key to City
        public int? CityId { get; set; }
        public City? City { get; set; }

        public string? PostCode { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string? PhoneNo { get; set; }
        public string? EmailId { get; set; }
        public string? BusinessCurrency { get; set; }
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserEmailId { get; set; }
        public string? Designation { get; set; }
        public string? MobileNo { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public bool AcceptTerms { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
