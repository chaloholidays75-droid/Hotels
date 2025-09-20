using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;
using HotelAPI.Models.DTO;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;

namespace HotelAPI.Controllers
{
    [Authorize]
    [Route("api/hotels")]
    [ApiController]
    public class HotelController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public HotelController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/hotels
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotelDto>>> GetHotels()
        {
            var hotels = await _context.HotelInfo
                                       .Where(h => h.IsActive)
                                       .Include(h => h.HotelStaff)
                                       .Include(h => h.City)
                                       .Include(h => h.Country)
                                       .ToListAsync();

            return Ok(_mapper.Map<List<HotelDto>>(hotels));
        }

        // GET: api/hotels/{id}
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("{id}")]
        public async Task<ActionResult<HotelDto>> GetHotel(int id)
        {
            var hotel = await _context.HotelInfo
                                      .Include(h => h.HotelStaff)
                                      .Include(h => h.City)
                                      .Include(h => h.Country)
                                      .FirstOrDefaultAsync(h => h.Id == id && h.IsActive);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            return Ok(_mapper.Map<HotelDto>(hotel));
        }

        // POST: api/hotels
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<ActionResult<HotelDto>> CreateHotel([FromBody] HotelDto dto)
        {
            var city = await _context.Cities
                                     .Include(c => c.Country)
                                     .FirstOrDefaultAsync(c => c.Id == dto.CityId && c.CountryId == dto.CountryId);

            if (city == null)
                return BadRequest(new { message = "City does not belong to the selected country" });

            var exists = await _context.HotelInfo
                                       .AnyAsync(h => h.HotelName == dto.HotelName
                                                   && h.Address == dto.Address
                                                   && h.CityId == dto.CityId
                                                   && h.IsActive);
            if (exists)
                return BadRequest(new { message = "Hotel with same details already exists" });

            var hotel = _mapper.Map<HotelInfo>(dto);
            hotel.City = city;
            hotel.Country = city.Country;
            hotel.IsActive = true;

            AddStaff(dto, hotel);

            _context.HotelInfo.Add(hotel);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHotel), new { id = hotel.Id }, _mapper.Map<HotelDto>(hotel));
        }

        // PUT: api/hotels/{id} (Admin only)
        // PUT: api/hotels/{id} (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHotel(int id, [FromBody] HotelDto dto)
        {
            var hotel = await _context.HotelInfo
                                    .Include(h => h.HotelStaff)
                                    .Include(h => h.City)
                                    .Include(h => h.Country)
                                    .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            var city = await _context.Cities
                                    .Include(c => c.Country)
                                    .FirstOrDefaultAsync(c => c.Id == dto.CityId && c.CountryId == dto.CountryId);

            if (city == null)
                return BadRequest("City does not belong to the selected country");

            // Update hotel fields
            hotel.HotelName = dto.HotelName;
            hotel.HotelEmail = dto.HotelEmail;
            hotel.HotelContactNumber = dto.HotelContactNumber;
            hotel.Address = dto.Address;
            hotel.Region = dto.Region;
            hotel.HotelChain = dto.HotelChain;
            hotel.SpecialRemarks = dto.SpecialRemarks;
            hotel.City = city;
            hotel.Country = city.Country;
            hotel.IsActive = dto.IsActive; // <-- update active/inactive status

            // Remove old staff
            _context.HotelStaff.RemoveRange(hotel.HotelStaff);
            hotel.HotelStaff.Clear();

            AddStaff(dto, hotel);

            hotel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // DELETE: api/hotels/{id} (Admin only, soft delete)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            var hotel = await _context.HotelInfo
                                    .Include(h => h.HotelStaff)
                                    .FirstOrDefaultAsync(h => h.Id == id && h.IsActive);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            // Set inactive instead of deleting
            hotel.IsActive = false;
            hotel.UpdatedAt = DateTime.UtcNow;

            _context.HotelInfo.Update(hotel);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // PATCH: api/hotels/{id}/status (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateHotelStatus(int id, [FromBody] bool isActive)
        {
            var hotel = await _context.HotelInfo.FindAsync(id);
            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            hotel.IsActive = isActive;
            hotel.UpdatedAt = DateTime.UtcNow;

            _context.HotelInfo.Update(hotel);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Hotel {(isActive ? "activated" : "deactivated")} successfully" });
        }

        // GET: api/hotels/by-city/{cityId}
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("by-city/{cityId}")]
        public async Task<ActionResult<IEnumerable<HotelDto>>> GetHotelsByCity(int cityId)
        {
            var hotels = await _context.HotelInfo
                                       .Where(h => h.CityId == cityId && h.IsActive)
                                       .ToListAsync();

            return Ok(_mapper.Map<List<HotelDto>>(hotels));
        }

        // Helper: Add staff
        private void AddStaff(HotelDto dto, HotelInfo hotel)
        {
            void MapStaff(List<HotelStaffDto>? staffDtos, string role)
            {
                if (staffDtos != null)
                {
                    foreach (var s in staffDtos)
                    {
                        var staff = _mapper.Map<HotelStaff>(s);
                        staff.Role = role;
                        staff.HotelInfo = hotel;
                        hotel.HotelStaff.Add(staff);
                    }
                }
            }

            MapStaff(dto.SalesPersons, "Sales");
            MapStaff(dto.ReceptionPersons, "Reception");
            MapStaff(dto.ReservationPersons, "Reservation");
            MapStaff(dto.AccountsPersons, "Accounts");
            MapStaff(dto.Concierges, "Concierge");
        }
    }
}
