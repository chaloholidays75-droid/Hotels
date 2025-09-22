using System;

namespace HotelAPI.Models
{
    public class Agency : AuditableEntity
    {
        public int Id { get; set; }
        public string? AgencyName { get; set; }

        public int? CountryId { get; set; }
        public Country? Country { get; set; }

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
        public bool IsActive { get; set; } = true;
        public string? Region { get; set; }
        public string? SpecialRemarks { get; set; }
    }

}
