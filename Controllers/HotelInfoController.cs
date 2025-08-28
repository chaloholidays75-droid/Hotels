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
                                       .ToListAsync();
            var hotelDtos = _mapper.Map<List<HotelDto>>(hotels);
            return Ok(hotelDtos);
        }

        // GET: api/hotels/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<HotelDto>> GetHotel(int id)
        {
            var hotel = await _context.HotelInfo
                                      .Include(h => h.HotelStaff)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            var hotelDto = _mapper.Map<HotelDto>(hotel);
            return Ok(hotelDto);
        }

        // POST: api/hotels
        [HttpPost]
        public async Task<ActionResult<HotelDto>> CreateHotel([FromBody] HotelDto dto)
        {
            // Check for duplicates (based on name, city, and address)
            var exists = await _context.HotelInfo
                .AnyAsync(h => h.HotelName == dto.HotelName && h.City == dto.City && h.Address == dto.Address);

            if (exists)
                return BadRequest(new { message = "Hotel with same details already exists" });

            var hotel = _mapper.Map<HotelInfo>(dto);

            // Add staff for each role
            void AddStaff(List<HotelStaffDto> staffDtos, string role)
            {
                if (staffDtos != null)
                {
                    foreach (var s in staffDtos)
                    {
                        var staff = _mapper.Map<HotelStaff>(s);
                        staff.Role = role;
                        hotel.HotelStaff.Add(staff);
                    }
                }
            }

            AddStaff(dto.SalesPersons, "Sales");
            AddStaff(dto.ReceptionPersons, "Reception");
            AddStaff(dto.ReservationPersons, "Reservation");
            AddStaff(dto.AccountsPersons, "Accounts");
            AddStaff(dto.Concierges, "Concierge");

            _context.HotelInfo.Add(hotel);
            await _context.SaveChangesAsync();

            var hotelDto = _mapper.Map<HotelDto>(hotel);
            return CreatedAtAction(nameof(GetHotel), new { id = hotel.Id }, hotelDto);
        }

        // PUT: api/hotels/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHotel(int id, [FromBody] HotelDto dto)
        {
            var hotel = await _context.HotelInfo
                                      .Include(h => h.HotelStaff)
                                      .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            // Update hotel properties
            hotel.Country = dto.Country;
            hotel.CountryCode = dto.CountryCode;
            hotel.City = dto.City;
            hotel.HotelName = dto.HotelName;
            hotel.HotelEmail = dto.HotelEmail;
            hotel.HotelContactNumber = dto.HotelContactNumber;
            hotel.Address = dto.Address;
            hotel.HotelChain = dto.HotelChain; // Added HotelChain
            hotel.SpecialRemarks = dto.SpecialRemarks;

            // Remove old staff
            _context.HotelStaff.RemoveRange(hotel.HotelStaff);
            hotel.HotelStaff.Clear();

            // Add new staff
            void AddStaff(List<HotelStaffDto> staffDtos, string role)
            {
                if (staffDtos != null)
                {
                    foreach (var s in staffDtos)
                    {
                        var staff = _mapper.Map<HotelStaff>(s);
                        staff.Role = role;
                        hotel.HotelStaff.Add(staff);
                    }
                }
            }

            AddStaff(dto.SalesPersons, "Sales");
            AddStaff(dto.ReceptionPersons, "Reception");
            AddStaff(dto.ReservationPersons, "Reservation");
            AddStaff(dto.AccountsPersons, "Accounts");
            AddStaff(dto.Concierges, "Concierge");

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
    }
}
