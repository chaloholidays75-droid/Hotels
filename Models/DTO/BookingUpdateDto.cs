namespace HotelAPI.Models.DTO
{
    // Add these DTO classes to your Controller or create a separate DTO file
    public class BookingUpdateDto
    {
        public int? AgencyId { get; set; }
        public int? AgencyStaffId { get; set; }
        public int? SupplierId { get; set; }
        public int? HotelId { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int? NumberOfRooms { get; set; }
        public string? Status { get; set; }
        public string? SpecialRequest { get; set; }
        public DateTime? Deadline { get; set; }

        public List<BookingRoomUpdateDto>? BookingRooms { get; set; }
    }

    public class BookingRoomUpdateDto
    {
        public int RoomTypeId { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }
        public List<int>? ChildrenAges { get; set; }
        public string? Inclusion { get; set; }
        public string? GuestName { get; set; }
    }
    
}