namespace HotelAPI.Models.DTO
{
    public class CountryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public List<CityDto> Cities { get; set; } = new();  
    }
}
