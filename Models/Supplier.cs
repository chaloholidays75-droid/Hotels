using System.ComponentModel.DataAnnotations;

namespace HotelAPI.Models
{
    public class Supplier : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

       
        public string SupplierName { get; set; } = null!;

        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        public int? CityId { get; set; }
        public City? City { get; set; }

        public string? PostCode { get; set; }
        public string? Address { get; set; }
        public string? Region { get; set; }
        public string? Website { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? BusinessCurrency { get; set; }

        // Relationships
        public int SupplierCategoryId { get; set; }
        public SupplierCategory SupplierCategory { get; set; } = null!;

        public int SupplierSubCategoryId { get; set; }
        public SupplierSubCategory SupplierSubCategory { get; set; } = null!;

        // Contact person
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Designation { get; set; }
        public string? MobileNo { get; set; }
        public string? UserEmailId { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }

        // Payment
        public bool EnablePaymentDetails { get; set; } = false;
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankSwiftCode { get; set; }
        public string? PaymentTerms { get; set; }
        public string? TaxId { get; set; }

        // Status
        public bool AcceptTerms { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string? SpecialRemarks { get; set; }
    }
}
