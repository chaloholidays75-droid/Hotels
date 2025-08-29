using HotelAPI.Models;

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;      // ISO code
    public string Flag { get; set; } = string.Empty;      // Emoji or URL
    public string PhoneCode { get; set; } = string.Empty; // e.g., +44
    public int PhoneNumberDigits { get; set; }
    public ICollection<City> Cities { get; set; } = new List<City>();
}
