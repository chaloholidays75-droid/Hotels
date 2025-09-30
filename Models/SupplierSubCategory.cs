using System.ComponentModel.DataAnnotations;

namespace HotelAPI.Models
{
    public class SupplierSubCategory : AuditableEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        // Foreign key to SupplierCategory
        public int SupplierCategoryId { get; set; }
        public SupplierCategory SupplierCategory { get; set; } = null!;

        // Navigation back to suppliers
        public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    }
}
