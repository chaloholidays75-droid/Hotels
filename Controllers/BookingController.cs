using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Authorization;
using HotelAPI.Models.DTO;

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
                    NumberOfPeople = b.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0)),
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
                NumberOfPeople = booking.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0)),
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
        public static class BookingConverters
        {
            public static string? AgesToString(List<int>? ages)
                => (ages == null || ages.Count == 0) ? null : string.Join(',', ages);

            public static List<int> StringToAges(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return new();
                return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => int.TryParse(x.Trim(), out var v) ? v : 0)
                        .ToList();
            }
        }
        private static DateTime EnsureUtc(DateTime dt)
        {
            // If client sent ISO8601 with Z, Kind will already be Utc.
            // If it's Unspecified, force to Utc to satisfy PostgreSQL (timestamptz).
            return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        private static string? AgesToString(List<int>? ages)
            => (ages == null || ages.Count == 0) ? null : string.Join(',', ages);

        private static List<int> StringToAges(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new();
            return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var v) ? v : 0)
                    .ToList();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookingCreateDto dto)
        {
            if (dto == null || dto.BookingRooms == null || !dto.BookingRooms.Any())
                return BadRequest(new { message = "Booking and at least one room are required." });

            // Force UTC to satisfy timestamptz columns (and avoid Npgsql errors)
            var checkInUtc  = EnsureUtc(dto.CheckIn);
            var checkOutUtc = EnsureUtc(dto.CheckOut);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) Create Booking (no temp ticket number)
                var booking = new Booking
                {
                    AgencyId = dto.AgencyId,
                    SupplierId = dto.SupplierId,
                    HotelId = dto.HotelId,
                    CheckIn = checkInUtc,
                    CheckOut = checkOutUtc,
                    Status  = string.IsNullOrWhiteSpace(dto.Status) ? "Confirmed" : dto.Status,
                    Deadline      = dto.Deadline.HasValue ? EnsureUtc(dto.Deadline.Value) : null,
                    NumberOfRooms = dto.BookingRooms.Count,
                    SpecialRequest = dto.SpecialRequest
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync(); // booking.Id is generated here

                // 2) Insert Rooms
                foreach (var roomDto in dto.BookingRooms)
                {
                    var room = new BookingRoom
                    {
                        BookingId     = booking.Id,
                        RoomTypeId    = roomDto.RoomTypeId,
                        Adults        = roomDto.Adults,
                        Children      = roomDto.Children,
                        ChildrenAges  = AgesToString(roomDto.ChildrenAges),
                        CreatedAt     = DateTime.UtcNow,
                        UpdatedAt     = DateTime.UtcNow
                    };
                    _context.BookingRooms.Add(room);
                }
                await _context.SaveChangesAsync();

                // 3) Auto-calc totals
                booking.NumberOfPeople = await _context.BookingRooms
                    .Where(r => r.BookingId == booking.Id)
                    .SumAsync(r => (r.Adults ?? 0) + (r.Children ?? 0));

                // Nights (server-side): whole days between in/out; clamp at >= 0
                var nights = (int)Math.Max(0, (checkOutUtc.Date - checkInUtc.Date).TotalDays);
                // If EF column is computed, DB may overwrite; at least include in response below.

                // 4) Final TicketNumber (your exact format)
                booking.TicketNumber = $"TICKET-{DateTime.UtcNow:yyyyMMddHHmm}-{booking.Id}";

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // 5) Return full, clean object (childrenAges as array)
                var result = await _context.Bookings
                    .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
                    .Include(b => b.Hotel)
                    .Include(b => b.Agency)
                    .Include(b => b.Supplier)
                    .Where(b => b.Id == booking.Id)
                    .Select(b => new
                    {
                        b.Id,
                        b.TicketNumber,
                        b.Status,
                        CheckIn  = b.CheckIn,
                        CheckOut = b.CheckOut,
                        Nights   = nights, // computed above; or b.Nights if your DB computes it
                        b.NumberOfRooms,
                        b.NumberOfPeople,
                        HotelName    = b.Hotel != null ? b.Hotel.HotelName : null,
                        AgencyName   = b.Agency != null ? b.Agency.AgencyName : null,
                        SupplierName = b.Supplier != null ? b.Supplier.SupplierName : null,
                        Rooms = b.BookingRooms.Select(r => new
                        {
                            r.Id,
                            r.RoomTypeId,
                            RoomTypeName = r.RoomType != null ? r.RoomType.Name : null,
                            r.Adults,
                            r.Children,
                            ChildrenAges = StringToAges(r.ChildrenAges)
                        })
                    })
                    .FirstAsync();

                return Ok(new
                {
                    message = "Booking created successfully",
                    booking = result
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Failed to create booking", error = ex.Message });
            }
        }


        // ------------------------
        // PUT: api/BookingManagement/{id}
        // ------------------------
 [HttpPut("{id}")]
public async Task<ActionResult<object>> Update(int id, [FromBody] BookingUpdateDto dto)
{
            if (dto == null)
                return BadRequest("Booking data is required");

     if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    var existing = await _context.Bookings
        .Include(b => b.BookingRooms)
        .FirstOrDefaultAsync(b => b.Id == id);

    if (existing == null)
        return NotFound();

    try
    {
        // ✅ Update main fields
        existing.HotelId = dto.HotelId ?? existing.HotelId;
        existing.AgencyId = dto.AgencyId ?? existing.AgencyId;
        existing.SupplierId = dto.SupplierId ?? existing.SupplierId;
        existing.CheckIn = dto.CheckIn.HasValue ? EnsureUtc(dto.CheckIn.Value) : existing.CheckIn;
        existing.CheckOut = dto.CheckOut.HasValue ? EnsureUtc(dto.CheckOut.Value) : existing.CheckOut;
        existing.Deadline = dto.Deadline ?? existing.Deadline;
        existing.Status = dto.Status ?? existing.Status;
        existing.SpecialRequest = dto.SpecialRequest ?? existing.SpecialRequest;
                if (!string.IsNullOrEmpty(dto.Status))
                {
                    existing.Status = dto.Status;

                    if (dto.Status == "Reconfirmed(Guaranteed)")
                    {
                        existing.Deadline = null; // ✅ clear on reconfirm
                    }
                    // No auto-cancel; no 24h logic
                }
                if (dto.Deadline.HasValue)
                {
                    existing.Deadline = EnsureUtc(dto.Deadline.Value);
                }
        // ✅ Replace old rooms safely
        _context.BookingRooms.RemoveRange(existing.BookingRooms);

        if (dto.BookingRooms != null && dto.BookingRooms.Any())
        {
            foreach (var roomDto in dto.BookingRooms)
            {
                _context.BookingRooms.Add(new BookingRoom
                {
                    BookingId = existing.Id,
                    RoomTypeId = roomDto.RoomTypeId,
                    Adults = roomDto.Adults,
                    Children = roomDto.Children,
                    ChildrenAges = AgesToString(roomDto.ChildrenAges),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // ✅ Auto update room count + people count
        existing.NumberOfRooms = dto.BookingRooms?.Count ?? existing.NumberOfRooms;
        existing.NumberOfPeople = dto.BookingRooms?.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0)) ?? existing.NumberOfPeople;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Booking updated successfully", booking = existing });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Failed to update booking", error = ex.Message });
    }
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
        // ------------------------
        // POST: api/BookingManagement/create-with-commercial
        // ------------------------
        [HttpPost("create-with-commercial")]
        public async Task<IActionResult> CreateWithCommercial([FromBody] BookingCommercialDTO dto)
        {
            if (dto == null || dto.Booking == null)
                return BadRequest(new { message = "Invalid request payload." });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1️⃣ Save Commercial first (if provided)
                Commercial? commercial = null;
                if (dto.Commercial != null)
                {
                    _context.Commercials.Add(dto.Commercial);
                    await _context.SaveChangesAsync();
                    commercial = dto.Commercial;
                }

                // 2️⃣ Prepare Booking
                var booking = dto.Booking;

                // Ensure UTC to prevent timestamptz errors
                // ✅ Ensure UTC only if values are not null
                if (booking.CheckIn.HasValue)
                {
                    booking.CheckIn = booking.CheckIn.Value.Kind == DateTimeKind.Utc
                        ? booking.CheckIn
                        : DateTime.SpecifyKind(booking.CheckIn.Value, DateTimeKind.Utc);
                }

                if (booking.CheckOut.HasValue)
                {
                    booking.CheckOut = booking.CheckOut.Value.Kind == DateTimeKind.Utc
                        ? booking.CheckOut
                        : DateTime.SpecifyKind(booking.CheckOut.Value, DateTimeKind.Utc);
                }

                booking.Status = string.IsNullOrEmpty(booking.Status) ? "Confirmed" : booking.Status;
                booking.TicketNumber = $"TICKET-{DateTime.UtcNow:yyyyMMddHHmm}-{Guid.NewGuid().ToString("N")[..5]}";

                // 3️⃣ Link Commercial if exists
                if (commercial != null)
                    booking.CommercialId = commercial.Id;

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // 4️⃣ Commit
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Booking and Commercial saved successfully.",
                    bookingId = booking.Id,
                    commercialId = commercial?.Id,
                    ticket = booking.TicketNumber
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Error saving booking with commercial data.",
                    error = ex.Message
                });
            }
        }
        // GET: api/booking/pending-reconfirmations
        [HttpGet("pending-reconfirmations")]
        public async Task<IActionResult> GetPendingReconfirmations()
        {
            var pending = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Agency)
                .Where(b => b.Status == "Confirmed" && b.Deadline.HasValue)
                .OrderBy(b => b.Deadline)
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
                    AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
                    b.Deadline
                })
                .ToListAsync();

            return Ok(pending);
            }


    }
}
