namespace HotelAPI.Models.DTO
{
    // DTO to create a request
    public class ChangeRequestDto
    {
        public string TargetEntity { get; set; } = string.Empty;
        public int TargetEntityId { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string? ChangeDetails { get; set; }
    }

    // DTO for admin to approve/reject
    public class ChangeRequestApprovalDto
    {
        public int RequestId { get; set; }
        public bool Approve { get; set; }
    }
}
