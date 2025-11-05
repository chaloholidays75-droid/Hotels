namespace HotelAPI.Models
{
    public class HotelStaff : AuditableEntity
    {
    public int Id { get; set; }
    public int? HotelInfoId { get; set; }
    public string? Role { get; set; } = string.Empty;  // e.g., "Reception", "Sales", "Concierge"
    public string? Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Contact { get; set; }

    public HotelInfo HotelInfo { get; set; } = null!;
    }
}