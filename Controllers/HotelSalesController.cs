using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;
using HotelAPI.Models.DTO;
namespace HotelAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HotelController(AppDbContext context)
        {
            _context = context;
        }

        // ===========================
        // GET: api/hotel
        // Get all hotels with their staff
        // ===========================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotelInfo>>> GetHotels()
        {
            var hotels = await _context.HotelInfos
                                       .Include(h => h.HotelStaff) // Include related staff
                                       .ToListAsync();
            return Ok(hotels);
        }

        // ===========================
        // GET: api/hotel/{id}
        // Get a single hotel with its staff
        // ===========================
        [HttpGet("{id}")]
        public async Task<ActionResult<HotelInfo>> GetHotel(int id)
        {
            var hotel = await _context.HotelInfos
                                      .Include(h => h.HotelStaff)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            return Ok(hotel);
        }

        // ===========================
        // POST: api/hotel
        // Create a new hotel with staff
        // ===========================
        [HttpPost]
        public async Task<ActionResult<HotelInfo>> CreateHotel([FromBody] HotelDto dto)
        {
            // Map DTO to entity
            var hotel = new HotelInfo
            {
                Country = dto.Country,
                CountryCode = dto.CountryCode,
                City = dto.City,
                HotelName = dto.HotelName,
                HotelContactNumber = dto.HotelContactNumber,
                Address = dto.Address,
                SpecialRemarks = dto.SpecialRemarks
            };

            // Map staff
            if (dto.Staff != null)
            {
                foreach (var s in dto.Staff)
                {
                    hotel.HotelStaff.Add(new HotelStaff
                    {
                        Role = s.Role,
                        Name = s.Name,
                        Email = s.Email,
                        Contact = s.Contact
                    });
                }
            }

            _context.HotelInfos.Add(hotel);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHotel), new { id = hotel.Id }, hotel);
        }

        // ===========================
        // PUT: api/hotel/{id}
        // Update a hotel and its staff
        // ===========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHotel(int id, [FromBody] HotelDto dto)
        {
            var hotel = await _context.HotelInfos
                                      .Include(h => h.HotelStaff)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            // Update hotel info
            hotel.Country = dto.Country;
            hotel.CountryCode = dto.CountryCode;
            hotel.City = dto.City;
            hotel.HotelName = dto.HotelName;
            hotel.HotelContactNumber = dto.HotelContactNumber;
            hotel.Address = dto.Address;
            hotel.SpecialRemarks = dto.SpecialRemarks;

            // Remove existing staff and add new (simple approach)
            _context.HotelStaffs.RemoveRange(hotel.HotelStaff);
            hotel.HotelStaff.Clear();

            if (dto.Staff != null)
            {
                foreach (var s in dto.Staff)
                {
                    hotel.HotelStaff.Add(new HotelStaff
                    {
                        Role = s.Role,
                        Name = s.Name,
                        Email = s.Email,
                        Contact = s.Contact
                    });
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ===========================
        // DELETE: api/hotel/{id}
        // Delete hotel and its staff
        // ===========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            var hotel = await _context.HotelInfos
                                      .Include(h => h.HotelStaff)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            // Remove staff first (foreign key constraints)
            _context.HotelStaffs.RemoveRange(hotel.HotelStaff);
            _context.HotelInfos.Remove(hotel);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
