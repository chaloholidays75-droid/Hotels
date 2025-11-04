using System;
using System.ComponentModel.DataAnnotations;

namespace HotelAPI.Models
{
    public class RecentActivity
    {
        [Key]
        public int Id { get; set; }


        public string? UserName { get; set; }

        [Required]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        public string TableName { get; set; } = string.Empty;

        public int? RecordId { get; set; }
        public string? Description { get; set; }
        public string? ChangedData { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
