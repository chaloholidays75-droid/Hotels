using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelAPI.Models
{
    public class ChangeRequest
    {
        [Key]
        public int Id { get; set; }

        // Who submitted the request
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public User Employee { get; set; } = null!;

        // Target table/entity
        public string TargetEntity { get; set; } = string.Empty; // e.g., "Hotel", "Agency"
        public int TargetEntityId { get; set; } // ID of hotel or agency

        // Type of request
        public string RequestType { get; set; } = string.Empty; // "edit" | "delete"

        // Optional: details of requested changes
        public string? ChangeDetails { get; set; }

        // Status
        public string Status { get; set; } = "pending"; // "pending" | "approved" | "rejected"
        public int? ReviewedBy { get; set; } // Admin Id
        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
