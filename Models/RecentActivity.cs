namespace HotelAPI.Models
{
    public class RecentActivity
    {
        public int Id { get; set; }
        public string Type { get; set; } // "hotel", "agency", "system"
        public string Action { get; set; } // "created", "updated", "deleted"
        public string Name { get; set; }
        public int CountryId { get; set; }
        // public Country? Country { get; set; }

        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; }
    }
}