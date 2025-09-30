using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelAPI.Models
{
    public class SupplierCategory : AuditableEntity
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        // Navigation
        public ICollection<SupplierSubCategory> SubCategories { get; set; } = new List<SupplierSubCategory>();
        public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    }
}
