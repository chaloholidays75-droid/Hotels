using HotelAPI.Models;

namespace HotelAPI.Models.DTO
{
    public class BookingCommercialDTO
    {
        public Booking Booking { get; set; } = default!;
        public Commercial? Commercial { get; set; }
    }
}
