namespace HotelAPI.Models.DTO
{
    public class BookingCreateDto
    {
        public int AgencyId { get; set; }
        public int SupplierId { get; set; }
        public int HotelId { get; set; }
        public int? NumberOfRooms { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }
        public int[] ChildrenAges { get; set; } = Array.Empty<int>();
        public string? SpecialRequest { get; set; }
        public int? NumberOfPeople { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
    }
}