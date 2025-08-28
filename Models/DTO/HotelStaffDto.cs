namespace HotelAPI.Models.DTO
{
    public class HotelStaffDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
    }
}
