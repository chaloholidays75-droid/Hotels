namespace HotelAPI.Models
{
    public class HotelStaffDto
    {
    public int Id { get; set; }
    public int HotelInfoId { get; set; }
    public string Role { get; set; } = string.Empty;  // e.g., "Reception", "Sales", "Concierge"
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;

    public HotelInfo HotelInfo { get; set; } = null!;
    }
}