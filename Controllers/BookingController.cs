using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        // ------------------------
        // GET: api/BookingManagement
        // ------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Hotel).ThenInclude(h => h.City)
                .Include(b => b.Hotel).ThenInclude(h => h.Country)
                .Include(b => b.Agency)
                .Include(b => b.Supplier)
                .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
                .OrderByDescending(b => b.Id)
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
                    AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
                    SupplierName = b.Supplier != null ? b.Supplier.SupplierName : null,
                    b.CheckIn,
                    b.CheckOut,
                    b.NumberOfRooms,
                    b.NumberOfPeople,
                    b.Status,
                    Rooms = b.BookingRooms.Select(br => new
                    {
                        br.Id,
                        br.RoomTypeId,
                        RoomTypeName = br.RoomType != null ? br.RoomType.Name : null,
                        br.Adults,
                        br.Children,
                        br.ChildrenAges
                    })
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // ------------------------
        // GET: api/BookingManagement/{id}
        // ------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Hotel).ThenInclude(h => h.City)
                .Include(b => b.Hotel).ThenInclude(h => h.Country)
                .Include(b => b.Agency)
                .Include(b => b.Supplier)
                .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            return Ok(new
            {
                booking.Id,
                booking.TicketNumber,
                HotelName = booking.Hotel?.HotelName,
                AgencyName = booking.Agency?.AgencyName,
                SupplierName = booking.Supplier?.SupplierName,
                booking.CheckIn,
                booking.CheckOut,
                booking.NumberOfRooms,
                booking.NumberOfPeople,
                booking.Status,
                Rooms = booking.BookingRooms.Select(r => new
                {
                    r.Id,
                    r.RoomTypeId,
                    RoomTypeName = r.RoomType?.Name,
                    r.Adults,
                    r.Children,
                    r.ChildrenAges
                })
            });
        }

        // ------------------------
        // POST: api/BookingManagement
        // ------------------------
[HttpPost]
public async Task<ActionResult<object>> Create([FromBody] Booking booking)
{
    if (booking == null)
        return BadRequest(new { message = "Invalid booking data." });

    // ✅ Duplicate prevention
    bool duplicateExists = await _context.Bookings.AnyAsync(b =>
        b.HotelId == booking.HotelId &&
        b.AgencyId == booking.AgencyId &&
        b.SupplierId == booking.SupplierId &&
        b.CheckIn == booking.CheckIn &&
        b.CheckOut == booking.CheckOut);

    if (duplicateExists)
        return Conflict(new { message = "A booking already exists for these dates with the same hotel, agency, and supplier." });

    // ✅ Assign a temporary non-null ticket number before saving
    booking.TicketNumber = $"TEMP-{Guid.NewGuid()}";
    booking.Status ??= "Pending";

    _context.Bookings.Add(booking);
    await _context.SaveChangesAsync();

    // ✅ Now assign the final formatted ticket number
    booking.TicketNumber = $"TICKET-{DateTime.UtcNow:yyyyMMddHHmm}-{booking.Id}";
    await _context.SaveChangesAsync();

    // ✅ Add related rooms
    if (booking.BookingRooms != null && booking.BookingRooms.Any())
    {
        foreach (var room in booking.BookingRooms)
        {
            // ✅ Handle new RoomType creation
            if (room.RoomType != null && room.RoomType.Id == 0)
            {
                // Make sure we have a valid HotelId before linking it
                if (booking.HotelId == null || booking.HotelId <= 0)
                    return BadRequest(new { message = "HotelId is required when creating a new RoomType." });

                // Verify that hotel actually exists
                bool hotelExists = await _context.HotelInfo.AnyAsync(h => h.Id == booking.HotelId);
                if (!hotelExists)
                    return BadRequest(new { message = $"Hotel with Id {booking.HotelId} does not exist." });

                room.RoomType.HotelId = booking.HotelId.Value;
                _context.RoomTypes.Add(room.RoomType);
                await _context.SaveChangesAsync();

                room.RoomTypeId = room.RoomType.Id;
            }


            room.BookingId = booking.Id;
            room.ChildrenAges = room.ChildrenAges?.Trim();
            _context.BookingRooms.Add(room);
        }

        await _context.SaveChangesAsync();
    }

    // ✅ Auto-calculate number of people
    booking.NumberOfPeople = booking.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0));
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
}


        // ------------------------
        // PUT: api/BookingManagement/{id}
        // ------------------------
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> Update(int id, [FromBody] Booking booking)
        {
            var existing = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (existing == null)
                return NotFound();

            existing.HotelId = booking.HotelId ?? existing.HotelId;
            existing.AgencyId = booking.AgencyId ?? existing.AgencyId;
            existing.SupplierId = booking.SupplierId ?? existing.SupplierId;
            existing.CheckIn = booking.CheckIn;
            existing.CheckOut = booking.CheckOut;
            existing.NumberOfRooms = booking.NumberOfRooms;
            existing.Status = booking.Status ?? existing.Status;

            _context.BookingRooms.RemoveRange(existing.BookingRooms);

            if (booking.BookingRooms != null && booking.BookingRooms.Any())
            {
                foreach (var room in booking.BookingRooms)
                {
                    if (room.RoomType != null && room.RoomType.Id == 0)
                    {
                        room.RoomType.HotelId = existing.HotelId ?? 0;
                        _context.RoomTypes.Add(room.RoomType);
                        await _context.SaveChangesAsync();
                        room.RoomTypeId = room.RoomType.Id;
                    }

                    room.BookingId = existing.Id;
                    _context.BookingRooms.Add(room);
                }
            }

            existing.NumberOfPeople = booking.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0));
            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        // ------------------------
        // DELETE: api/BookingManagement/{id}
        // ------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            _context.BookingRooms.RemoveRange(booking.BookingRooms);
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking deleted successfully." });
        }

        // ------------------------
        // SEARCH: api/BookingManagement/search?query=...
        // ------------------------
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Please provide a search query." });

            query = query.ToLower();

            var results = await _context.Bookings
                .Include(b => b.Hotel).ThenInclude(h => h.City)
                .Include(b => b.Hotel).ThenInclude(h => h.Country)
                .Include(b => b.Agency)
                .Include(b => b.Supplier)
                .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
                .Where(b =>
                    (b.Hotel != null &&
                    (b.Hotel.HotelName.ToLower().Contains(query) ||
                     b.Hotel.HotelChain.ToLower().Contains(query) ||
                     b.Hotel.Address.ToLower().Contains(query) ||
                     (b.Hotel.City != null && b.Hotel.City.Name.ToLower().Contains(query)) ||
                     (b.Hotel.Country != null && b.Hotel.Country.Name.ToLower().Contains(query))))
                )
                .OrderByDescending(b => b.Id)
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    HotelName = b.Hotel.HotelName,
                    CityName = b.Hotel.City.Name,
                    CountryName = b.Hotel.Country.Name,
                    b.CheckIn,
                    b.CheckOut,
                    b.Status,
                    b.NumberOfRooms
                })
                .ToListAsync();

            return Ok(results);
        }

        // ------------------------
        // PAGED: api/BookingManagement/paged?page=1&pageSize=10
        // ------------------------
        [HttpGet("paged")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Invalid pagination parameters." });

            var results = await _context.Bookings
                .Include(b => b.Hotel).ThenInclude(h => h.City)
                .Include(b => b.Agency)
                .OrderByDescending(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    HotelName = b.Hotel.HotelName,
                    CityName = b.Hotel.City.Name,
                    b.CheckIn,
                    b.CheckOut,
                    b.Status
                })
                .ToListAsync();

            return Ok(results);
        }

        // ------------------------
        // HOTELS AUTOCOMPLETE: api/BookingManagement/hotels-autocomplete?query=...
        // ------------------------
        [HttpGet("hotels-autocomplete")]
        public async Task<ActionResult<IEnumerable<object>>> HotelsAutocomplete([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query cannot be empty." });

            query = query.ToLower();

            var hotels = await _context.HotelInfo
                .Include(h => h.City)
                .Include(h => h.Country)
                .Where(h =>
                    h.HotelName.ToLower().Contains(query) ||
                    h.Address.ToLower().Contains(query) ||
                    (h.City != null && h.City.Name.ToLower().Contains(query)) ||
                    (h.Country != null && h.Country.Name.ToLower().Contains(query))
                )
                .OrderBy(h => h.HotelName)
                .Select(h => new
                {
                    h.Id,
                    h.HotelName,
                    CityName = h.City.Name,
                    CountryName = h.Country.Name
                })
                .Take(10)
                .ToListAsync();

            return Ok(hotels);
        }
    }
}
