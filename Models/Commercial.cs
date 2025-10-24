using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelAPI.Models
{
    public class Commercial : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        // ✅ Optional link to Booking
        public int? BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        // -------- BUYING SIDE --------
        [MaxLength(10)]
        public string BuyingCurrency { get; set; } = "USD";
        public decimal BuyingAmount { get; set; }
        public bool Commissionable { get; set; }
        [MaxLength(20)]
        public string CommissionType { get; set; } = "percentage";
        public decimal? CommissionValue { get; set; }
        public bool BuyingVatIncluded { get; set; }
        public decimal BuyingVatPercent { get; set; } = 18;
        public string? AdditionalCostsJson { get; set; }

        // -------- SELLING SIDE --------
        [MaxLength(10)]
        public string SellingCurrency { get; set; } = "USD";
        public decimal SellingPrice { get; set; }
        public bool Incentive { get; set; }
        [MaxLength(20)]
        public string IncentiveType { get; set; } = "percentage";
        public decimal? IncentiveValue { get; set; }
        public bool SellingVatIncluded { get; set; }
        public decimal SellingVatPercent { get; set; } = 18;
        public string? DiscountsJson { get; set; }

        // -------- EXCHANGE / SUMMARY --------
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
}
