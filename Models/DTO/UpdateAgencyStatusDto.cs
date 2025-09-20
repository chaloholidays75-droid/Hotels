namespace HotelAPI.Models.DTO
{
    public class UpdateAgencyStatusDto
    {
        public int AgencyId { get; set; }
        public bool IsActive { get; set; }
    }
}