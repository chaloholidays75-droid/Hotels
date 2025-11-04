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
        private readonly ILogger<BookingController> _logger;

        public BookingController(AppDbContext context, ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ------------------------
        // GET: api/Booking
        // ------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            _logger.LogInformation("Fetching all bookings");

            try
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
                            br.Inclusion,
                            br.GuestName,
                            br.ChildrenAges
                        })
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} bookings successfully", bookings.Count);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, "Error fetching all bookings");
            }
        }

        // ------------------------
        // GET: api/Booking/{id}
        // ------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            _logger.LogInformation("Fetching booking by Id: {Id}", id);

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Hotel).ThenInclude(h => h.City)
                    .Include(b => b.Hotel).ThenInclude(h => h.Country)
                    .Include(b => b.Agency)
                    .Include(b => b.Supplier)
                    .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    _logger.LogWarning("Booking not found for Id {Id}", id);
                    return NotFound();
                }

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
                        r.Inclusion,
                        r.GuestName,
                        r.ChildrenAges
                    })
                });
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, $"Error fetching booking with Id {id}");
            }
        }

        // ------------------------
        // Converters / Helpers
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
            => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        private static string? AgesToString(List<int>? ages)
            => (ages == null || ages.Count == 0) ? null : string.Join(',', ages);

        private static List<int> StringToAges(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new();
            return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var v) ? v : 0)
                    .ToList();
        }

        // ------------------------
        // POST: api/Booking
        // ------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookingCreateDto dto)
        {
            _logger.LogInformation("Creating new booking");

            if (dto == null || dto.BookingRooms == null || !dto.BookingRooms.Any())
                return BadRequest(new { message = "Booking and at least one room are required." });

            // Force UTC for timestamptz columns
            var checkInUtc = EnsureUtc(dto.CheckIn);
            var checkOutUtc = EnsureUtc(dto.CheckOut);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1) Create Booking
                var booking = new Booking
                {
                    AgencyId = dto.AgencyId,
                    SupplierId = dto.SupplierId,
                    HotelId = dto.HotelId,
                    CheckIn = checkInUtc,
                    CheckOut = checkOutUtc,
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Confirmed" : dto.Status,
                    Deadline = dto.Deadline.HasValue ? EnsureUtc(dto.Deadline.Value) : null,
                    NumberOfRooms = dto.BookingRooms.Count,
                    SpecialRequest = dto.SpecialRequest
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync(); // booking.Id generated

                // 2) Insert Rooms (with Inclusion + GuestName)
                foreach (var roomDto in dto.BookingRooms)
                {
                    var room = new BookingRoom
                    {
                        BookingId = booking.Id,
                        RoomTypeId = roomDto.RoomTypeId,
                        Adults = roomDto.Adults,
                        Children = roomDto.Children,
                        Inclusion = roomDto.Inclusion ?? string.Empty,
                        GuestName = roomDto.GuestName ?? string.Empty,
                        ChildrenAges = AgesToString(roomDto.ChildrenAges),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.BookingRooms.Add(room);
                }
                await _context.SaveChangesAsync();

                // 3) Auto totals
                booking.NumberOfPeople = await _context.BookingRooms
                    .Where(r => r.BookingId == booking.Id)
                    .SumAsync(r => (r.Adults ?? 0) + (r.Children ?? 0));

                // Nights (>=0)
                var nights = (int)Math.Max(0, (checkOutUtc.Date - checkInUtc.Date).TotalDays);

                // 4) Final ticket number
                booking.TicketNumber = $"TICKET-{DateTime.UtcNow:yyyyMMddHHmm}-{booking.Id}";

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Booking {BookingId} created successfully", booking.Id);

                // 5) Return full, clean object
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
                        CheckIn = b.CheckIn,
                        CheckOut = b.CheckOut,
                        Nights = nights,
                        b.NumberOfRooms,
                        b.NumberOfPeople,
                        HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
                        AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
                        SupplierName = b.Supplier != null ? b.Supplier.SupplierName : null,
                        Rooms = b.BookingRooms.Select(r => new
                        {
                            r.Id,
                            r.RoomTypeId,
                            RoomTypeName = r.RoomType != null ? r.RoomType.Name : null,
                            r.Adults,
                            r.Children,
                            r.Inclusion,
                            r.GuestName,
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
                return BuildErrorResponse(ex, "Failed to create booking");
            }
        }

        // ------------------------
        // PUT: api/Booking/{id}
        // ------------------------
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> Update(int id, [FromBody] BookingUpdateDto dto)
        {
            _logger.LogInformation("Updating booking Id: {Id}", id);

            if (dto == null)
                return BadRequest("Booking data is required");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
                        existing.Deadline = null; // clear on reconfirm
                    }
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
                            Inclusion = roomDto.Inclusion ?? string.Empty,
                            GuestName = roomDto.GuestName ?? string.Empty,
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

                _logger.LogInformation("Booking {Id} updated successfully", id);
                return Ok(new { message = "Booking updated successfully", booking = existing });
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, $"Failed to update booking Id {id}");
            }
        }

        // ------------------------
        // DELETE: api/Booking/{id}
        // ------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting booking Id: {Id}", id);

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingRooms)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    _logger.LogWarning("Booking not found for deletion. Id: {Id}", id);
                    return NotFound();
                }

                _context.BookingRooms.RemoveRange(booking.BookingRooms);
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Booking {Id} deleted successfully", id);
                return Ok(new { message = "Booking deleted successfully." });
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, $"Error deleting booking Id {id}");
            }
        }

        // ------------------------
        // SEARCH: api/Booking/search?query=...
        // ------------------------
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> Search([FromQuery] string query)
        {
            _logger.LogInformation("Searching bookings. Query: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Please provide a search query." });

            query = query.ToLower();

            try
            {
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

                _logger.LogInformation("Search found {Count} bookings", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, "Error searching bookings");
            }
        }

        // ------------------------
        // PAGED: api/Booking/paged?page=1&pageSize=10
        // ------------------------
        [HttpGet("paged")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Getting paged bookings. Page: {Page}, Size: {Size}", page, pageSize);

            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Invalid pagination parameters." });

            try
            {
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

                _logger.LogInformation("Paged result count: {Count}", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, "Error fetching paged bookings");
            }
        }

        // ------------------------
        // HOTELS AUTOCOMPLETE: api/Booking/hotels-autocomplete?query=...
        // ------------------------
        [HttpGet("hotels-autocomplete")]
        public async Task<ActionResult<IEnumerable<object>>> HotelsAutocomplete([FromQuery] string query)
        {
            _logger.LogInformation("Hotels autocomplete. Query: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query cannot be empty." });

            query = query.ToLower();

            try
            {
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

                _logger.LogInformation("Hotels autocomplete returned {Count} results", hotels.Count);
                return Ok(hotels);
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, "Error in hotels autocomplete");
            }
        }

        // ------------------------
        // POST: api/Booking/create-with-commercial
        // ------------------------
        [HttpPost("create-with-commercial")]
        public async Task<IActionResult> CreateWithCommercial([FromBody] BookingCommercialDTO dto)
        {
            _logger.LogInformation("Creating booking with commercial payload");

            if (dto == null || dto.Booking == null)
                return BadRequest(new { message = "Invalid request payload." });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1) Save Commercial first (if provided)
                Commercial? commercial = null;
                if (dto.Commercial != null)
                {
                    _context.Commercials.Add(dto.Commercial);
                    await _context.SaveChangesAsync();
                    commercial = dto.Commercial;
                }

                // 2) Prepare Booking
                var booking = dto.Booking;

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

                // 3) Link Commercial if exists
                if (commercial != null)
                    booking.CommercialId = commercial.Id;

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // 4) Commit
                await transaction.CommitAsync();

                _logger.LogInformation("Booking-with-commercial created. BookingId: {Id}, CommercialId: {CommercialId}", booking.Id, commercial?.Id);

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
                return BuildErrorResponse(ex, "Error saving booking with commercial data");
            }
        }

        // ------------------------
        // GET: api/Booking/pending-reconfirmations
        // ------------------------
        [HttpGet("pending-reconfirmations")]
        public async Task<IActionResult> GetPendingReconfirmations()
        {
            _logger.LogInformation("Fetching pending reconfirmations");

            try
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

                _logger.LogInformation("Pending reconfirmations count: {Count}", pending.Count);
                return Ok(pending);
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, "Error fetching pending reconfirmations");
            }
        }

        // ------------------------
        // Centralized error response & logging
        // ------------------------
        private ObjectResult BuildErrorResponse(Exception ex, string context)
        {
            var inner = ex.InnerException?.Message ?? "No inner exception";
            var stack = ex.StackTrace ?? "No stack trace";

            _logger.LogError(ex, "❌ {Context} | Error: {Error} | Inner: {Inner}", context, ex.Message, inner);

            return StatusCode(500, new
            {
                message = context,
                error = ex.Message,
                inner = inner,
                stackTrace = stack
            });
        }
    }
}
