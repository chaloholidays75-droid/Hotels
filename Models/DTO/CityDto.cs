namespace HotelAPI.Models.DTO
{
    public class CityDto
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
