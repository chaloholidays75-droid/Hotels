namespace HotelAPI.Models.DTO
{
    public class BookingRoomDto
    {
        public int Id { get; set; }

        public int RoomTypeId { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public List<int>? ChildrenAges { get; set; } = new List<int>();
        public string? Inclusion { get; set; }
        public string? LeadGuestName { get; set; }
        public List<string>? GuestNames { get; set; } = new();


        // Auto-calculated property
        public int NumberOfPeople => Adults + Children;
    }
}
