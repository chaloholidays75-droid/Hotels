namespace HotelAPI.Models.DTO
{
    public class SupplierRequestDto
    {
        public string? SupplierName { get; set; }
        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public string? PostCode { get; set; }
        public string? Address { get; set; }
        public string? Region { get; set; }
        public string? Website { get; set; }
        public int? SupplierCategoryId { get; set; }
        public int? SupplierSubCategoryId { get; set; }
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
        public string? SpecialRemarks { get; set; }
        public bool? EnablePaymentDetails { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? PaymentTerms { get; set; }
    }
}
