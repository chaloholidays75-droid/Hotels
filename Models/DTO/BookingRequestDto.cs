namespace HotelAPI.Models.DTO
{
    public class BookingCreateDto
    {
        public int AgencyId { get; set; }
        public int? AgencyStaffId { get; set; } 
        public int SupplierId { get; set; }
        public int HotelId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int? NumberOfRooms { get; set; }
        public string? SpecialRequest { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Status { get; set; } = "Confirmed";
        public CancellationPolicyDto? CancellationPolicy { get; set; }
        public List<BookingRoomDto> BookingRooms { get; set; } = new List<BookingRoomDto>();

    }
}
// Add this new DTO
public class CancellationPolicyDto
{
    public string PolicyType { get; set; } = "free_cancellation";
    public string? CustomName { get; set; }
    public List<CancellationRuleDto> Rules { get; set; } = new();
}

public class CancellationRuleDto
{
    public string Type { get; set; } = "free_cancellation_before";
    public int Days { get; set; }
    public string Charge { get; set; } = "0";
    public string GuestType { get; set; } = "FIT";
}