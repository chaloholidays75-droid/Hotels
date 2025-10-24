namespace HotelAPI.Models.DTO
{
    public class AgencyStaffReadDto
    {
        public int Id { get; set; }
        public int AgencyId { get; set; }
        public string? AgencyName { get; set; } // ✅ NEW
        public string? Role { get; set; }
        public string? Designation { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? CreatedById { get; set; }
        public int? UpdatedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
