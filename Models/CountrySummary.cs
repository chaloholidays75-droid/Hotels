public class CountrySummary
{
    public int CountryId { get; set; }
    public string? CountryName { get; set; }
    public string? CountryCode { get; set; }
    public int HotelCount { get; set; }
    public int AgencyCount { get; set; }
    public int TotalEntities => HotelCount + AgencyCount;
}