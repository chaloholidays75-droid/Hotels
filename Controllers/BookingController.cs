using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;
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

        // ============================================================
        // Helpers
        // ============================================================

        private static DateTime EnsureUtc(DateTime dt)
            => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        private static string? AgesToString(List<int>? ages)
            => (ages == null ) ? null : string.Join(',', ages);

        private static List<int> StringToAges(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new();
            return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var v) ? v : 0)
                .ToList();
        }

        private static string DetectBookingType(int? hotelId, int? supplierId)
        {
            if (hotelId.HasValue && hotelId.Value > 0) return "H";
            if (supplierId.HasValue && supplierId.Value > 0) return "S";
            return "T";
        }

        // ============================================================
        // POST: api/Booking  (Create Booking)
        // ============================================================
[HttpPost]
public async Task<IActionResult> Create([FromBody] BookingCreateDto dto)
{
    _logger.LogInformation("Creating new booking...");

    if (dto == null || dto.BookingRooms == null || !dto.BookingRooms.Any())
        return BadRequest(new { message = "Booking and at least one room are required." });

    if (dto.Deadline.HasValue && dto.Deadline.Value >= dto.CheckIn)
        return BadRequest(new { message = "Deadline must be before Check-In." });

    var checkInUtc = EnsureUtc(dto.CheckIn);
    var checkOutUtc = EnsureUtc(dto.CheckOut);

    await using var tx = await _context.Database.BeginTransactionAsync();

    try
    {
        string bookingType = DetectBookingType(dto.HotelId, dto.SupplierId);
        string bookingReference = await _context.GenerateBookingReferenceAsync(bookingType);
        if (string.IsNullOrWhiteSpace(bookingReference))
            return StatusCode(500, new { message = "Failed to generate BookingReference." });

        string ticketNumber = $"Booking-{DateTime.UtcNow:yyyyMMddHHmm}-{bookingReference}";

        var booking = new Booking
        {
            AgencyId = dto.AgencyId,
            AgencyStaffId = dto.AgencyStaffId,
            SupplierId = dto.SupplierId,
            HotelId = dto.HotelId,
            CheckIn = checkInUtc,
            CheckOut = checkOutUtc,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Confirmed" : dto.Status,
            Deadline = dto.Deadline.HasValue ? EnsureUtc(dto.Deadline.Value) : null,
            NumberOfRooms = dto.BookingRooms.Count,
            SpecialRequest = dto.SpecialRequest,
            BookingType = bookingType,
            BookingReference = bookingReference,
            TicketNumber = ticketNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // Store cancellation policy as JSON
            CancellationPolicyJson = dto.CancellationPolicy != null 
                ? System.Text.Json.JsonSerializer.Serialize(dto.CancellationPolicy)
                : null
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        foreach (var room in dto.BookingRooms)
        {
            var newRoom = new BookingRoom
            {
                BookingId = booking.Id,
                RoomTypeId = room.RoomTypeId,
                Adults = room.Adults,
                Children = room.Children,
                Inclusion = room.Inclusion ?? "",
                LeadGuestName = room.LeadGuestName,
                GuestNames = room.GuestNames ?? new List<string>(),
                ChildrenAges = AgesToString(room.ChildrenAges),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BookingRooms.Add(newRoom);
        }

        await _context.SaveChangesAsync();

        booking.NumberOfPeople = await _context.BookingRooms
            .Where(r => r.BookingId == booking.Id)
            .SumAsync(r => (r.Adults ?? 0) + (r.Children ?? 0));

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { message = "Booking created successfully", bookingId = booking.Id });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return BuildErrorResponse(ex, "Failed to create booking");
    }
}

        // ============================================================
        // GET: api/Booking  (Get All Bookings)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Hotel)
                    .Include(b => b.Agency)
                    .Include(b => b.Supplier)
                    .OrderByDescending(b => b.Id)
                    .ToListAsync();

                var allRooms = await _context.BookingRooms
                    .Include(r => r.RoomType)
                    .ToListAsync();

                var result = bookings.Select(booking =>
                {
                    var rooms = allRooms.Where(r => r.BookingId == booking.Id).ToList();

                    return new
                    {
                        booking.Id,
                        booking.BookingType,
                        booking.BookingReference,
                        booking.TicketNumber,
                        HotelName = booking.Hotel?.HotelName,
                        AgencyName = booking.Agency?.AgencyName,
                        SupplierName = booking.Supplier?.SupplierName,
                        booking.CheckIn,
                        booking.CheckOut,
                        booking.NumberOfRooms,
                        booking.SpecialRequest,
                        NumberOfPeople = rooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0)),
                        booking.Status,
                        Nights = booking.CheckIn.HasValue && booking.CheckOut.HasValue
                            ? (int)(booking.CheckOut.Value - booking.CheckIn.Value).TotalDays
                            : 0,

                        BookingRooms = rooms.Select(r => new
                        {
                            r.Id,
                            r.RoomTypeId,
                            r.RoomType?.Name,
                            r.Adults,
                            r.Children,
                            r.LeadGuestName,
                            GuestNames = r.GuestNames ?? new List<string>(),
                            ChildrenAges = StringToAges(r.ChildrenAges)
                        })
                    };
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, "Error fetching all bookings");
            }
        }

        // ============================================================
        // GET: api/Booking/{id}  (Get Booking By Id)
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
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
                    return NotFound();

                return Ok(new
                {
                    booking.Id,
                    booking.BookingType,
                    booking.BookingReference,
                    booking.TicketNumber,
                    booking.HotelId,
                    booking.AgencyId,
                    booking.SupplierId,
                    booking.AgencyStaffId,
                    booking.CheckIn,
                    booking.CheckOut,
                    booking.Deadline,
                    booking.SpecialRequest,
                    booking.Status,
                    booking.NumberOfRooms,
                    NumberOfPeople = booking.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0)),

                    Rooms = booking.BookingRooms.Select(r => new
                    {
                        r.Id,
                        r.RoomTypeId,
                        RoomTypeName = r.RoomType?.Name,
                        r.Adults,
                        r.Children,
                        r.Inclusion,
                        r.LeadGuestName,
                        GuestNames = r.GuestNames,
                        ChildrenAges = StringToAges(r.ChildrenAges)

                    })
                });
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, $"Error fetching booking Id {id}");
            }
        }

        // ============================================================
        // PUT: api/Booking/{id} (Update booking)
        // ============================================================
[HttpPut("{id}")]
public async Task<IActionResult> Update(int id, [FromBody] BookingUpdateDto dto)
{
    _logger.LogInformation("Updating booking Id: {Id}", id);

    var existing = await _context.Bookings
        .Include(b => b.BookingRooms)
        .FirstOrDefaultAsync(b => b.Id == id);

    if (existing == null)
        return NotFound();

    try
    {
        // ✅ Update main booking fields
        existing.HotelId = dto.HotelId ?? existing.HotelId;
        existing.AgencyId = dto.AgencyId ?? existing.AgencyId;
        existing.AgencyStaffId = dto.AgencyStaffId ?? existing.AgencyStaffId;
        existing.SupplierId = dto.SupplierId ?? existing.SupplierId;

        if (dto.CheckIn.HasValue)
            existing.CheckIn = EnsureUtc(dto.CheckIn.Value);

        if (dto.CheckOut.HasValue)
            existing.CheckOut = EnsureUtc(dto.CheckOut.Value);

        if (!string.IsNullOrEmpty(dto.SpecialRequest))
            existing.SpecialRequest = dto.SpecialRequest;

        if (!string.IsNullOrEmpty(dto.Status))
        {
            existing.Status = dto.Status;
            if (dto.Status == "Reconfirmed(Guaranteed)")
                existing.Deadline = null; // clear deadline
        }

        if (dto.Deadline.HasValue)
        {
            if (existing.CheckIn.HasValue && dto.Deadline.Value >= existing.CheckIn.Value)
                return BadRequest(new { message = "Deadline must be before Check-In." });

            existing.Deadline = EnsureUtc(dto.Deadline.Value);
        }

        // ✅ Safe UPDATE logic for BookingRooms
        var existingRooms = existing.BookingRooms.ToList();

        // ✅ Update or Create Rooms
        foreach (var roomDto in dto.BookingRooms)
        {
            if (roomDto.Id.HasValue)
            {
                // ✅ UPDATE EXISTING ROOM
                var room = existingRooms.FirstOrDefault(r => r.Id == roomDto.Id.Value);
                if (room == null)
                    continue;

                room.RoomTypeId = roomDto.RoomTypeId ?? room.RoomTypeId;
                room.Adults = roomDto.Adults;
                room.Children = roomDto.Children;

                // ✅ Convert ages list correctly
                room.ChildrenAges = AgesToString(roomDto.ChildrenAges ?? new List<int>());

                room.Inclusion = roomDto.Inclusion ?? room.Inclusion;
                room.LeadGuestName = roomDto.LeadGuestName;

                // ✅ GuestNames received as LIST → stored as LIST (no conversion)
                room.GuestNames = roomDto.GuestNames ?? new List<string>();

                room.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // ✅ ADD NEW ROOM
                _context.BookingRooms.Add(new BookingRoom
                {
                    BookingId = existing.Id,
                    RoomTypeId = roomDto.RoomTypeId.Value,
                    Adults = roomDto.Adults,
                    Children = roomDto.Children,
                    ChildrenAges = AgesToString(roomDto.ChildrenAges),
                    Inclusion = roomDto.Inclusion ?? "",
                    LeadGuestName = roomDto.LeadGuestName,
                    GuestNames = roomDto.GuestNames ?? new List<string>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // ✅ DELETE rooms not in DTO
        var dtoIds = dto.BookingRooms.Where(r => r.Id.HasValue).Select(r => r.Id.Value).ToList();
        var toDelete = existingRooms.Where(r => !dtoIds.Contains(r.Id)).ToList();

        if (toDelete.Any())
            _context.BookingRooms.RemoveRange(toDelete);

        // ✅ Update summary fields
        existing.NumberOfRooms = dto.BookingRooms.Count;
        existing.NumberOfPeople = dto.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0));

        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking {Id} updated successfully", id);
        return Ok(new { message = "Booking updated successfully." });
    }
    catch (Exception ex)
    {
        return BuildErrorResponse(ex, $"Failed to update booking Id {id}");
    }
}


        // ============================================================
        // DELETE: api/Booking/{id}
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingRooms)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                    return NotFound();

                _context.BookingRooms.RemoveRange(booking.BookingRooms);
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Booking deleted successfully" });
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, $"Error deleting booking Id {id}");
            }
        }         // ============================================================
        // GET: api/Booking/search?query=...
        // ============================================================
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            _logger.LogInformation("Searching bookings. Query: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query cannot be empty." });

            query = query.ToLower();

            try
            {
                var results = await _context.Bookings
                    .Include(b => b.Hotel)
                    .Include(b => b.Agency)
                    .Where(b =>
                        (b.Hotel != null && b.Hotel.HotelName.ToLower().Contains(query)) ||
                        (b.Agency != null && b.Agency.AgencyName.ToLower().Contains(query)) ||
                        (!string.IsNullOrEmpty(b.TicketNumber) && b.TicketNumber.ToLower().Contains(query)) ||
                        (!string.IsNullOrEmpty(b.BookingReference) && b.BookingReference.ToLower().Contains(query)))
                    .OrderByDescending(b => b.Id)
                    .Select(b => new
                    {
                        b.Id,
                        b.BookingType,
                        b.BookingReference,
                        b.TicketNumber,
                        HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
                        AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
                        b.Status,
                        b.CheckIn,
                        b.CheckOut
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

        // ============================================================
        // GET: api/Booking/paged?page=1&pageSize=10
        // ============================================================
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Getting paged bookings. Page: {Page}, Size: {Size}", page, pageSize);

            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Invalid pagination parameters." });

            try
            {
                var total = await _context.Bookings.CountAsync();

                var results = await _context.Bookings
                    .Include(b => b.Hotel)
                    .Include(b => b.Agency)
                    .OrderByDescending(b => b.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        b.Id,
                        b.BookingType,
                        b.BookingReference,
                        b.TicketNumber,
                        HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
                        AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
                        b.Status,
                        b.CheckIn,
                        b.CheckOut
                    })
                    .ToListAsync();

                return Ok(new { total, page, pageSize, results });
            }
            catch (Exception ex)
            {
                return BuildErrorResponse(ex, "Error fetching paged bookings");
            }
        }

        // ============================================================
        // GET: api/Booking/hotels-autocomplete?query=...
        // ============================================================
        [HttpGet("hotels-autocomplete")]
        public async Task<IActionResult> HotelsAutocomplete([FromQuery] string query)
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
                        CityName = h.City != null ? h.City.Name : null,
                        CountryName = h.Country != null ? h.Country.Name : null
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

        // ============================================================
        // POST: api/Booking/create-with-commercial
        // ============================================================
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
                    booking.CheckIn = EnsureUtc(booking.CheckIn.Value);
                if (booking.CheckOut.HasValue)
                    booking.CheckOut = EnsureUtc(booking.CheckOut.Value);

                // Auto booking type/ref/ticket here as well (mirrors Create)
                string bType = DetectBookingType(booking.HotelId, booking.SupplierId);
                booking.BookingType = bType;
                booking.BookingReference = await _context.GenerateBookingReferenceAsync(bType);
                if (string.IsNullOrWhiteSpace(booking.BookingReference))
                    return StatusCode(500, new { message = "Failed to generate BookingReference." });

                booking.TicketNumber = $"BOOKING-{DateTime.UtcNow:yyyyMMddHHmm}-{booking.BookingReference}";
                booking.Status = string.IsNullOrEmpty(booking.Status) ? "Confirmed" : booking.Status;

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

        // ============================================================
        // GET: api/Booking/pending-reconfirmations
        // ============================================================
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
                        b.BookingType,
                        b.BookingReference,
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

 

        [HttpPatch("Booking/{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateBookingStatusRequest req)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = req.Status;
            booking.AgentVoucher = req.AgentVoucher;
            
            booking.CancelReason = req.CancelReason;

            await _context.SaveChangesAsync();
            return Ok(booking);
        }




        // ============================================================
        // ERROR HANDLER (FULL DETAILED LOGGING)
        // ============================================================
        private ObjectResult BuildErrorResponse(Exception ex, string context)
        {
            var inner1 = ex.InnerException?.Message ?? "null";
            var inner2 = ex.InnerException?.InnerException?.Message ?? "null";
            var inner3 = ex.InnerException?.InnerException?.InnerException?.Message ?? "null";

            var stack = ex.StackTrace ?? "null";

            _logger.LogError(ex,
                "❌ {Context}\n" +
                "Error: {Error}\n" +
                "Inner1: {Inner1}\n" +
                "Inner2: {Inner2}\n" +
                "Inner3: {Inner3}\n" +
                "Source: {Source}\n" +
                "Target: {Target}\n" +
                "Stack: {Stack}",
                context, ex.Message, inner1, inner2, inner3,
                ex.Source, ex.TargetSite?.Name, stack
            );

            return StatusCode(500, new
            {
                message = context,
                error = ex.Message,
                inner1,
                inner2,
                inner3,
                type = ex.GetType().FullName,
                source = ex.Source,
                target = ex.TargetSite?.Name,
                stackTrace = stack
            });
        }
        
    }
}
