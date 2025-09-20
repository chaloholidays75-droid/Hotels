using System;

namespace HotelAPI.Models
{
    // Interface
    public interface IAuditable
    {
        int? CreatedById { get; set; }
        int? UpdatedById { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
    }

    // Abstract base class
    public abstract class AuditableEntity : IAuditable
    {
        public int? CreatedById { get; set; }
        public int? UpdatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
