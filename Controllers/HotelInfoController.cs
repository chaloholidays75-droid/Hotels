using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;
using HotelAPI.Models.DTO;
using AutoMapper;

namespace HotelAPI.Controllers
{
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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotelDto>>> GetHotels()
        {
            var hotels = await _context.HotelInfo
                                       .Include(h => h.HotelStaff)
                                       .Include(h => h.City)
                                       .Include(h => h.Country)
                                       .ToListAsync();

            return Ok(_mapper.Map<List<HotelDto>>(hotels));
        }

        // GET: api/hotels/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<HotelDto>> GetHotel(int id)
        {
            var hotel = await _context.HotelInfo
                                      .Include(h => h.HotelStaff)
                                      .Include(h => h.City)
                                      .Include(h => h.Country)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            return Ok(_mapper.Map<HotelDto>(hotel));
        }

        // POST: api/hotels
[HttpPost]
public async Task<ActionResult<HotelDto>> CreateHotel([FromBody] HotelDto dto)
{
    // Validate city belongs to country
    var city = await _context.Cities
                             .Include(c => c.Country)
                             .FirstOrDefaultAsync(c => c.Id == dto.CityId && c.CountryId == dto.CountryId);

    if (city == null)
        return BadRequest(new { message = "City does not belong to the selected country" });

    // Prevent duplicate hotels
    var exists = await _context.HotelInfo
                               .AnyAsync(h => h.HotelName == dto.HotelName
                                           && h.Address == dto.Address
                                           && h.CityId == dto.CityId);
    if (exists)
        return BadRequest(new { message = "Hotel with same details already exists" });

    // Map DTO to HotelInfo
    var hotel = _mapper.Map<HotelInfo>(dto);
    hotel.City = city;
    hotel.Country = city.Country;

    // Add staff manually
    void AddStaff(List<HotelStaffDto>? staffDtos, string role)
    {
        if (staffDtos != null)
        {
            foreach (var s in staffDtos)
            {
                var staff = _mapper.Map<HotelStaff>(s);
                staff.Role = role;
                staff.HotelInfo = hotel; // ✅ Important: assign parent
                hotel.HotelStaff.Add(staff);
            }
        }
    }

    AddStaff(dto.SalesPersons, "Sales");
    AddStaff(dto.ReceptionPersons, "Reception");
    AddStaff(dto.AccountsPersons, "Accounts");
    AddStaff(dto.Concierges, "Concierge");
    AddStaff(dto.ReservationPersons, "Reservation");

    // Save everything in one go
    _context.HotelInfo.Add(hotel);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetHotel), new { id = hotel.Id }, _mapper.Map<HotelDto>(hotel));
}

        // PUT: api/hotels/{id}
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

            // ✅ Validate city belongs to country
            var city = await _context.Cities
                                     .Include(c => c.Country)
                                     .FirstOrDefaultAsync(c => c.Id == dto.CityId && c.CountryId == dto.CountryId);

            if (city == null)
                return BadRequest("City does not belong to the selected country");

            // Update hotel details
            hotel.HotelName = dto.HotelName;
            hotel.HotelEmail = dto.HotelEmail;
            hotel.HotelContactNumber = dto.HotelContactNumber;
            hotel.Address = dto.Address;
            hotel.HotelChain = dto.HotelChain;
            hotel.SpecialRemarks = dto.SpecialRemarks;
            hotel.City = city;
            hotel.Country = city.Country;

            // Remove old staff
            _context.HotelStaff.RemoveRange(hotel.HotelStaff);
            hotel.HotelStaff.Clear();

            AddStaff(dto, hotel);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/hotels/{id}
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

        // Helper: Add staff without breaking existing logic
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
        [HttpGet("by-city/{cityId}")]
        public async Task<ActionResult<IEnumerable<HotelDto>>> GetHotelsByCity(int cityId)
        {
            var hotels = await _context.HotelInfo
                                       .Where(h => h.CityId == cityId)
                                       .ToListAsync();

            return Ok(_mapper.Map<List<HotelDto>>(hotels));
        }

    }
}