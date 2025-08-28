using HotelAPI.Models;

public class HotelInfo
{
    public int Id { get; set; }
    public int CountryId { get; set; }
    public int CityId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelChain { get; set; } = string.Empty;
    public string HotelEmail { get; set; } = string.Empty;
    public string HotelContactNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string SpecialRemarks { get; set; } = string.Empty;

   
    public City City { get; set; } = null!;
    public Country Country { get; set; } = null!;


    public ICollection<HotelStaff> HotelStaff { get; set; } = new List<HotelStaff>();
}
