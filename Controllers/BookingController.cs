using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Models;
using HotelAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        // GET: api/Booking
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Agency)
                .Include(b => b.Supplier)
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.City)
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.Country)
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
                    SupplierName = b.Supplier != null ? b.Supplier.SupplierName : null,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
                    HotelChain = b.Hotel != null ? b.Hotel.HotelChain : null,
                    Address = b.Hotel != null ? b.Hotel.Address : null,
                    Region = b.Hotel != null ? b.Hotel.Region : null,
                    CityName = b.Hotel != null && b.Hotel.City != null ? b.Hotel.City.Name : null,
                    CountryName = b.Hotel != null && b.Hotel.Country != null ? b.Hotel.Country.Name : null,
                    b.CheckIn,
                    b.CheckOut,
                    b.NumberOfRooms,
                    b.Adults,
                    b.Children,
                    b.ChildrenAges,
                     b.NumberOfPeople,
                    b.Status,
                    b.SpecialRequest,
                    Nights = (b.CheckIn.HasValue && b.CheckOut.HasValue)
                        ? (int)(b.CheckOut.Value - b.CheckIn.Value).TotalDays
                        : 0
                })
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return Ok(bookings);
        }

        // GET: api/Booking/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Agency)
                .Include(b => b.Supplier)
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.City)
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.Country)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            var nights = (booking.CheckIn.HasValue && booking.CheckOut.HasValue)
                ? (int)(booking.CheckOut.Value - booking.CheckIn.Value).TotalDays
                : 0;

            return Ok(new
            {
                booking.Id,
                booking.TicketNumber,
                AgencyName = booking.Agency != null ? booking.Agency.AgencyName : null,
                SupplierName = booking.Supplier != null ? booking.Supplier.SupplierName : null,
                HotelName = booking.Hotel != null ? booking.Hotel.HotelName : null,
                HotelChain = booking.Hotel != null ? booking.Hotel.HotelChain : null,
                Address = booking.Hotel != null ? booking.Hotel.Address : null,
                Region = booking.Hotel != null ? booking.Hotel.Region : null,
                CityName = booking.Hotel != null && booking.Hotel.City != null ? booking.Hotel.City.Name : null,
                CountryName = booking.Hotel != null && booking.Hotel.Country != null ? booking.Hotel.Country.Name : null,
                booking.CheckIn,
                booking.CheckOut,
                booking.NumberOfRooms,
                booking.Adults,
                booking.Children,
                booking.ChildrenAges,
                booking.Status,
                booking.SpecialRequest,
                Nights = nights
            });
        }

        // SEARCH: api/Booking/search?query=paris
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchBookings([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Please provide a search query." });

            query = query.ToLower();

            var bookings = await _context.Bookings
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.City)
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.Country)
                .Include(b => b.Agency)
                .Include(b => b.Supplier)
                .Where(b =>
                    (b.Hotel != null &&
                        (b.Hotel.HotelName.ToLower().Contains(query) ||
                         b.Hotel.HotelChain.ToLower().Contains(query) ||
                         b.Hotel.Address.ToLower().Contains(query) ||
                         (b.Hotel.Region != null && b.Hotel.Region.ToLower().Contains(query)) ||
                         (b.Hotel.City != null && b.Hotel.City.Name.ToLower().Contains(query)) ||
                         (b.Hotel.Country != null && b.Hotel.Country.Name.ToLower().Contains(query))
                        )
                    )
                )
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
                    HotelChain = b.Hotel != null ? b.Hotel.HotelChain : null,
                    Address = b.Hotel != null ? b.Hotel.Address : null,
                    Region = b.Hotel != null ? b.Hotel.Region : null,
                    CityName = b.Hotel != null && b.Hotel.City != null ? b.Hotel.City.Name : null,
                    CountryName = b.Hotel != null && b.Hotel.Country != null ? b.Hotel.Country.Name : null,
                    AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
                    SupplierName = b.Supplier != null ? b.Supplier.SupplierName : null,
                    b.CheckIn,
                    b.CheckOut,
                    b.Status
                })
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return Ok(bookings);
        }

        // PAGINATION: api/Booking/paged?page=1&pageSize=20
        [HttpGet("paged")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Invalid pagination parameters." });

            var bookings = await _context.Bookings
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.City)
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.Country)
                .Include(b => b.Agency)
                .Include(b => b.Supplier)
                .OrderByDescending(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
                    HotelChain = b.Hotel != null ? b.Hotel.HotelChain : null,
                    Address = b.Hotel != null ? b.Hotel.Address : null,
                    Region = b.Hotel != null ? b.Hotel.Region : null,
                    CityName = b.Hotel != null && b.Hotel.City != null ? b.Hotel.City.Name : null,
                    CountryName = b.Hotel != null && b.Hotel.Country != null ? b.Hotel.Country.Name : null,
                    AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
                    SupplierName = b.Supplier != null ? b.Supplier.SupplierName : null,
                    b.CheckIn,
                    b.CheckOut,
                    b.Status
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // POST: api/Booking
        [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] Booking booking)
    {
        if (booking.CheckIn.HasValue)
            booking.CheckIn = DateTime.SpecifyKind(booking.CheckIn.Value, DateTimeKind.Utc);
        if (booking.CheckOut.HasValue)
            booking.CheckOut = DateTime.SpecifyKind(booking.CheckOut.Value, DateTimeKind.Utc);

        bool duplicateExists = await _context.Bookings.AnyAsync(b =>
            b.AgencyId == booking.AgencyId &&
            b.SupplierId == booking.SupplierId &&
            b.HotelId == booking.HotelId &&
            b.CheckIn == booking.CheckIn &&
            b.CheckOut == booking.CheckOut
        );

        if (duplicateExists)
            return Conflict(new { message = "Booking already exists for these dates." });

        if (booking.CheckIn.HasValue && booking.CheckOut.HasValue)
            booking.Nights = (int)(booking.CheckOut.Value - booking.CheckIn.Value).TotalDays;

        booking.Status ??= "Pending";
        booking.TicketNumber = "TEMP-" + Guid.NewGuid();

        // 🧮 Automatically calculate total number of people
        if (booking.Adults.HasValue || booking.Children.HasValue)
            booking.NumberOfPeople = (booking.Adults ?? 0) + (booking.Children ?? 0);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        booking.CreatedById = userId;
        booking.UpdatedById = userId;


        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        booking.TicketNumber = $"TICKET-{DateTime.UtcNow:yyyyMMddHHmm}-{booking.Id}";
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
    }

        // PUT: api/Booking/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Booking>> Update(int id, [FromBody] Booking booking)
        {
            var existingBooking = await _context.Bookings.FindAsync(id);
            if (existingBooking == null)
                return NotFound();

            if (booking.CheckIn.HasValue)
                booking.CheckIn = DateTime.SpecifyKind(booking.CheckIn.Value, DateTimeKind.Utc);
            if (booking.CheckOut.HasValue)
                booking.CheckOut = DateTime.SpecifyKind(booking.CheckOut.Value, DateTimeKind.Utc);

            bool isDuplicate = await _context.Bookings.AnyAsync(b =>
                b.Id != id &&
                b.AgencyId == booking.AgencyId &&
                b.SupplierId == booking.SupplierId &&
                b.HotelId == booking.HotelId &&
                booking.CheckIn < b.CheckOut &&
                booking.CheckOut > b.CheckIn
            );

            if (isDuplicate)
                return Conflict("Another booking overlaps for these entities.");

            existingBooking.AgencyId = booking.AgencyId;
            existingBooking.SupplierId = booking.SupplierId;
            existingBooking.HotelId = booking.HotelId;
            existingBooking.CheckIn = booking.CheckIn;
            existingBooking.CheckOut = booking.CheckOut;
            existingBooking.NumberOfRooms = booking.NumberOfRooms;
            existingBooking.Adults = booking.Adults;
            existingBooking.Children = booking.Children;
            existingBooking.ChildrenAges = booking.ChildrenAges;
            existingBooking.SpecialRequest = booking.SpecialRequest;
            existingBooking.Status = booking.Status ?? existingBooking.Status;
            existingBooking.UpdatedAt = DateTime.UtcNow;

            // var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // booking.UpdatedById = userId;

            if (existingBooking.CheckIn.HasValue && existingBooking.CheckOut.HasValue)
                existingBooking.Nights = (int)(existingBooking.CheckOut.Value - existingBooking.CheckIn.Value).TotalDays;

            await _context.SaveChangesAsync();

            return Ok(existingBooking);
        }

        // DELETE: api/Booking/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return NotFound();

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted successfully" });
        }
        // GET: api/Booking/hotels-autocomplete?query=p
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
            h.HotelChain.ToLower().Contains(query) ||
            h.Address.ToLower().Contains(query) ||
            (h.Region != null && h.Region.ToLower().Contains(query)) ||
            (h.City != null && h.City.Name.ToLower().Contains(query)) ||
            (h.Country != null && h.Country.Name.ToLower().Contains(query))
        )
        .OrderBy(h => h.HotelName)
        .Select(h => new
        {
            h.Id,
            h.HotelName,
            h.HotelChain,
            h.Address,
            Region = h.Region,
            CityName = h.City != null ? h.City.Name : null,
            CountryName = h.Country != null ? h.Country.Name : null
        })
        .Take(10) // top 10 suggestions
        .ToListAsync();

    return Ok(hotels);
}

    }
}
