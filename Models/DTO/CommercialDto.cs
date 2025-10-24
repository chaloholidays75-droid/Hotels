namespace HotelAPI.Models.DTO
{
    public class CommercialCreateDto
    {
        public int BookingId { get; set; }

        // Buying
        public string BuyingCurrency { get; set; }
        public decimal BuyingAmount { get; set; }
        public bool Commissionable { get; set; }
        public string CommissionType { get; set; }
        public decimal? CommissionValue { get; set; }
        public bool BuyingVatIncluded { get; set; }
        public decimal BuyingVatPercent { get; set; }
        public string? AdditionalCostsJson { get; set; }

        // Selling
        public string SellingCurrency { get; set; }
        public decimal SellingPrice { get; set; }
        public bool Incentive { get; set; }
        public string IncentiveType { get; set; }
        public decimal? IncentiveValue { get; set; }
        public bool SellingVatIncluded { get; set; }
        public decimal SellingVatPercent { get; set; }
        public string? DiscountsJson { get; set; }

        // Summary
        public decimal? ExchangeRate { get; set; }
        public bool AutoCalculateRate { get; set; }
        public decimal GrossBuying { get; set; }
        public decimal NetBuying { get; set; }
        public decimal GrossSelling { get; set; }
        public decimal NetSelling { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMarginPercent { get; set; }
        public decimal MarkupPercent { get; set; }
    }

    public class CommercialUpdateDto
    {
          public int? BookingId { get; set; }
        public string? BuyingCurrency { get; set; }
        public string? SellingCurrency { get; set; }
        public decimal? BuyingAmount { get; set; }
        public decimal? SellingPrice { get; set; }
        public bool? Commissionable { get; set; }
        public string? CommissionType { get; set; }
        public decimal? CommissionValue { get; set; }
        public bool? Incentive { get; set; }
        public string? IncentiveType { get; set; }
        public decimal? IncentiveValue { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? Profit { get; set; }
        public decimal? ProfitMarginPercent { get; set; }
        public decimal? MarkupPercent { get; set; }
    }
}
