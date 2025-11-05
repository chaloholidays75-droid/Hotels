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

        public List<BookingRoomDto> BookingRooms { get; set; } = new List<BookingRoomDto>();

    }
}
