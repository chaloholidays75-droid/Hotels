namespace HotelAPI.Models
{
    public class RecentActivity
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string ActionType { get; set; } = ""; // Add, Edit, Delete
        public string Entity { get; set; } = "";     // e.g. Agency, Hotel
        public int EntityId { get; set; }
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}