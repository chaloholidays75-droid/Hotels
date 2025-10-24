namespace HotelAPI.Models.DTO
{
    public class AgencyStaffItemDto
    {
        public string Role { get; set; } = "";           // e.g., "Sales"
        public string? Designation { get; set; }          // optional
        public string Name { get; set; } = "";            // required
        public string? Email { get; set; }                // optional
        public string? Phone { get; set; }                // optional
    }
}
