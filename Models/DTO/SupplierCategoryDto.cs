using HotelAPI.Models.DTO;

public class SupplierCategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    // Include the subcategories
    public List<SupplierSubCategoryDto> SubCategories { get; set; } = new List<SupplierSubCategoryDto>();
}
