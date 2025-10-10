public class BookingRoomDto
{
    public int Id { get; set; }
   
    public int RoomTypeId { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public List<int> ChildrenAges { get; set; } = new List<int>();

    // Auto-calculated property
    public int NumberOfPeople => Adults + Children;
}
