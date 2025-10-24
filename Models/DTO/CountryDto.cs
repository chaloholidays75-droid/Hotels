namespace HotelAPI.Models.DTO
{
    public class CountryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty; // optional if you want flag
        public string PhoneCode { get; set; } = string.Empty;
        public int PhoneNumberDigits { get; set; }
        public string Region { get; set; } = string.Empty;
        public List<CityDto> Cities { get; set; } = new();  
    }
}
