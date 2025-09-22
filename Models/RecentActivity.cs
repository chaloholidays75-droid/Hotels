namespace HotelAPI.Models
{
    public class RecentActivity
    {
        public int Id { get; set; }              // SERIAL primary key
        public int UserId { get; set; }          // Who performed the action
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // e.g., CREATE, UPDATE
        public string? Entity { get; set; }      // e.g., Hotel, Booking
        public int? EntityId { get; set; }       // Id of affected entity
        public string? Description { get; set; } // Optional details
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }


}