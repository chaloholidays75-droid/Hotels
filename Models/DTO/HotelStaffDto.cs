namespace HotelAPI.Models.DTO
{
    // DTO for HotelStaff (used in API requests/responses)
    public class HotelStaffDto
    {
        public int Id { get; set; }
        public int HotelInfoId { get; set; }
        public string Role { get; set; } = string.Empty;  // e.g., Reception, Sales
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
    }
}
