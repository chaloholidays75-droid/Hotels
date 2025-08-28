using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;
using HotelAPI.Models.DTO;

namespace HotelAPI.Controllers
{
    [Route("api/hotels")]
    [ApiController]
    public class HotelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HotelController(AppDbContext context)
        {
            _context = context;
        }

        // ===========================
        // GET: api/hotels
        // Get all hotels with their staff
        // ===========================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotelInfo>>> GetHotels()
        {
            var hotels = await _context.HotelInfo
                                       .Include(h => h.HotelStaff)
                                       .ToListAsync();
            return Ok(hotels);
        }

        // ===========================
        // GET: api/hotels/{id}
        // Get a single hotel with its staff
        // ===========================
        [HttpGet("{id}")]
        public async Task<ActionResult<HotelInfo>> GetHotel(int id)
        {
            var hotel = await _context.HotelInfo
                                      .Include(h => h.HotelStaff)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            return Ok(hotel);
        }

        // ===========================
        // POST: api/hotels
        // Create a new hotel with staff
        // ===========================
        [HttpPost]
        public async Task<ActionResult<HotelInfo>> CreateHotel([FromBody] HotelDto dto)
        {
            // 1️⃣ Check for duplicate hotels (same name + city + country OR HotelEmail OR contact number)
            var exists = await _context.HotelInfo
                .AnyAsync(h => (h.HotelName == dto.HotelName
                                && h.City == dto.City
                                && h.CountryCode == dto.CountryCode)
                              || h.HotelEmail == dto.HotelEmail
                              || h.HotelContactNumber == dto.HotelContactNumber);

            if (exists)
                return Conflict(new { message = "Hotel with same details already exists" });

            // 2️⃣ Map DTO to HotelInfo entity
            var hotel = new HotelInfo
            {
                Country = dto.Country,
                CountryCode = dto.CountryCode,
                City = dto.City,
                HotelName = dto.HotelName,
                HotelContactNumber = dto.HotelContactNumber,
                HotelEmail = dto.HotelEmail,
                Address = dto.Address,
                SpecialRemarks = dto.SpecialRemarks
            };

            // 3️⃣ Add staff while avoiding duplicates by HotelEmail
            if (dto.HotelStaff != null)
            {
                var HotelEmails = new HashSet<string>();
                foreach (var s in dto.HotelStaff)
                {
                    if (!HotelEmails.Contains(s.Email))
                    {
                        hotel.HotelStaff.Add(new HotelStaff
                        {
                            Role = s.Role,
                            Name = s.Name,
                            Email = s.Email,
                            Contact = s.Contact,
                            HotelInfo = hotel
                        });
                        HotelEmails.Add(s.Email);
                    }
                }
            }

            // 4️⃣ Save hotel with staff
            _context.HotelInfo.Add(hotel);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHotel), new { id = hotel.Id }, hotel);
        }

        // ===========================
        // PUT: api/hotels/{id}
        // Update a hotel and its staff
        // ===========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHotel(int id, [FromBody] HotelDto dto)
        {
            var hotel = await _context.HotelInfo
                                      .Include(h => h.HotelStaff)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            // 1️⃣ Check for duplicates (ignore current hotel)
            var duplicate = await _context.HotelInfo
                .AnyAsync(h => h.Id != id &&
                               ((h.HotelName == dto.HotelName
                                 && h.City == dto.City
                                 && h.CountryCode == dto.CountryCode)
                                || h.HotelEmail == dto.HotelEmail
                                || h.HotelContactNumber == dto.HotelContactNumber));

            if (duplicate)
                return Conflict(new { message = "Another hotel with same details exists" });

            // 2️⃣ Update hotel info
            hotel.Country = dto.Country;
            hotel.CountryCode = dto.CountryCode;
            hotel.City = dto.City;
            hotel.HotelName = dto.HotelName;
            hotel.HotelContactNumber = dto.HotelContactNumber;
            hotel.HotelEmail = dto.HotelEmail;
            hotel.Address = dto.Address;
            hotel.SpecialRemarks = dto.SpecialRemarks;

            // 3️⃣ Remove existing staff
            _context.HotelStaff.RemoveRange(hotel.HotelStaff);
            hotel.HotelStaff.Clear();

            // 4️⃣ Add new staff, avoiding duplicates by HotelEmail
            if (dto.HotelStaff != null)
            {
                var HotelEmails = new HashSet<string>();
                foreach (var s in dto.HotelStaff)
                {
                    if (!HotelEmails.Contains(s.Email))
                    {
                        hotel.HotelStaff.Add(new HotelStaff
                        {
                            Role = s.Role,
                            Name = s.Name,
                            Email = s.Email,
                            Contact = s.Contact,
                            HotelInfo = hotel
                        });
                        HotelEmails.Add(s.Email);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ===========================
        // DELETE: api/hotels/{id}
        // Delete hotel and its staff
        // ===========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            var hotel = await _context.HotelInfo
                                      .Include(h => h.HotelStaff)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            _context.HotelStaff.RemoveRange(hotel.HotelStaff);
            _context.HotelInfo.Remove(hotel);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
