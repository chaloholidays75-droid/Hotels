// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using HotelAPI.Data;
// using HotelAPI.Models;
// using HotelAPI.Models.DTO;

// namespace HotelAPI.Controllers
// {
//     [ApiController]
//     [Authorize]
//     [Route("api/[controller]")]
//     public class BookingController : ControllerBase
//     {
//         private readonly AppDbContext _context;
//         private readonly ILogger<BookingController> _logger;

//         public BookingController(AppDbContext context, ILogger<BookingController> logger)
//         {
//             _context = context;
//             _logger = logger;
//         }

//         // ============================================================
//         // Helpers
//         // ============================================================
//         private static DateTime EnsureUtc(DateTime dt)
//             => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

//         private static string? AgesToString(List<int>? ages)
//             => (ages == null || ages.Count == 0) ? null : string.Join(',', ages);

//         private static List<int> StringToAges(string? s)
//         {
//             if (string.IsNullOrWhiteSpace(s)) return new();
//             return s.Split(',', StringSplitOptions.RemoveEmptyEntries)
//                     .Select(x => int.TryParse(x.Trim(), out var v) ? v : 0)
//                     .ToList();
//         }

//         private static string DetectBookingType(int? hotelId, int? supplierId)
//         {
//             if (hotelId.HasValue && hotelId.Value > 0) return "H";  // Hotel
//             if (supplierId.HasValue && supplierId.Value > 0) return "S"; // Service
//             return "T"; // Transport (fallback only if neither Hotel nor Service present)
//         }

//         // ============================================================
//         // POST: api/Booking
//         // ============================================================
//         [HttpPost]
//         public async Task<IActionResult> Create([FromBody] BookingCreateDto dto)
//         {
//             _logger.LogInformation("Creating new booking");

//             if (dto == null || dto.BookingRooms == null || !dto.BookingRooms.Any())
//                 return BadRequest(new { message = "Booking and at least one room are required." });

//             if (dto.Deadline.HasValue && dto.Deadline.Value >= dto.CheckIn)
//                 return BadRequest(new { message = "Deadline must be before Check-In." });

//             var checkInUtc = EnsureUtc(dto.CheckIn);
//             var checkOutUtc = EnsureUtc(dto.CheckOut);

//             await using var tx = await _context.Database.BeginTransactionAsync();

//             try
//             {
//                 // 1️⃣ Determine booking type
//                 string bookingType = DetectBookingType(dto.HotelId, dto.SupplierId);

//                 // 2️⃣ Generate Booking Reference
//                 string bookingReference = await _context.GenerateBookingReferenceAsync(bookingType);
//                 if (string.IsNullOrWhiteSpace(bookingReference))
//                     return StatusCode(500, new { message = "Failed to generate BookingReference." });

//                 // 3️⃣ Generate Ticket Number
//                 string ticketNumber = $"Booking-{DateTime.UtcNow:yyyyMMddHHmm}-{bookingReference}";

//                 // 4️⃣ Create main booking entity
//                 var booking = new Booking
//                 {
//                     AgencyId = dto.AgencyId,
//                     AgencyStaffId = dto.AgencyStaffId,
//                     SupplierId = dto.SupplierId,
//                     HotelId = dto.HotelId,
//                     CheckIn = checkInUtc,
//                     CheckOut = checkOutUtc,
//                     Status = string.IsNullOrWhiteSpace(dto.Status) ? "Confirmed" : dto.Status,
//                     Deadline = dto.Deadline.HasValue ? EnsureUtc(dto.Deadline.Value) : null,
//                     NumberOfRooms = dto.BookingRooms.Count,
//                     SpecialRequest = dto.SpecialRequest,
//                     BookingType = bookingType,
//                     BookingReference = bookingReference,
//                     TicketNumber = ticketNumber,
//                     CreatedAt = DateTime.UtcNow,
//                     UpdatedAt = DateTime.UtcNow
//                 };

//                 _context.Bookings.Add(booking);
//                 await _context.SaveChangesAsync(); // Generate booking.Id

//                 // 5️⃣ Add related rooms
//                 foreach (var roomDto in dto.BookingRooms)
//                 {
//                     var room = new BookingRoom
//                     {
//                         BookingId = booking.Id,
//                         RoomTypeId = roomDto.RoomTypeId,
//                         Adults = roomDto.Adults,
//                         Children = roomDto.Children,
//                         Inclusion = roomDto.Inclusion ?? string.Empty,
//                         LeadGuestName = roomDto.LeadGuestName,
//                         GuestNames = roomDto.GuestNames ?? new List<string>(),
//                         ChildrenAges = AgesToString(roomDto.ChildrenAges),
//                         CreatedAt = DateTime.UtcNow,
//                         UpdatedAt = DateTime.UtcNow
//                     };
//                     _context.BookingRooms.Add(room);
//                 }

//                 await _context.SaveChangesAsync();

//                 // 6️⃣ Update totals
//                 booking.NumberOfPeople = await _context.BookingRooms
//                     .Where(r => r.BookingId == booking.Id)
//                     .SumAsync(r => (r.Adults ?? 0) + (r.Children ?? 0));

//                 await _context.SaveChangesAsync();
//                 await tx.CommitAsync();

//                 // 7️⃣ Fetch booking (after commit) — in memory projection
//                 var bookingLoaded = await _context.Bookings
//                     .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
//                     .Include(b => b.Hotel).ThenInclude(h => h.City)
//                     .Include(b => b.Hotel).ThenInclude(h => h.Country)
//                     .Include(b => b.Agency)
//                     .Include(b => b.Supplier)
//                     .FirstAsync(b => b.Id == booking.Id);

//                 var result = new
//                 {
//                     bookingLoaded.Id,
//                     bookingLoaded.BookingType,
//                     bookingLoaded.BookingReference,
//                     bookingLoaded.TicketNumber,
//                     bookingLoaded.Status,
//                     bookingLoaded.CheckIn,
//                     bookingLoaded.CheckOut,
//                     Nights = (bookingLoaded.CheckIn.HasValue && bookingLoaded.CheckOut.HasValue)
//                         ? (int)Math.Max(0, (bookingLoaded.CheckOut.Value.Date - bookingLoaded.CheckIn.Value.Date).TotalDays)
//                         : 0,
//                     bookingLoaded.NumberOfRooms,
//                     bookingLoaded.NumberOfPeople,
//                     HotelName = bookingLoaded.Hotel?.HotelName,
//                     CityName = bookingLoaded.Hotel?.City?.Name,
//                     CountryName = bookingLoaded.Hotel?.Country?.Name,
//                     AgencyName = bookingLoaded.Agency?.AgencyName,
//                     AgencyStaffName = bookingLoaded.AgencyStaff?.Name,
//                     SupplierName = bookingLoaded.Supplier?.SupplierName,
//                     Rooms = bookingLoaded.BookingRooms.Select(r => new
//                     {
//                         r.Id,
//                         r.RoomTypeId,
//                         RoomTypeName = r.RoomType?.Name,
//                         r.Adults,
//                         r.Children,
//                         r.Inclusion,
//                         r.LeadGuestName,
//                         r.GuestNames,
//                         ChildrenAges = StringToAges(r.ChildrenAges)
//                     })
//                 };

//                 return Ok(new { message = "Booking created successfully", booking = result });
//             }
//             catch (Exception ex)
//             {
//                 await tx.RollbackAsync();
//                 return BuildErrorResponse(ex, "Failed to create booking");
//             }
//         }


//         // ============================================================
//         // GET: api/Booking
//         // ============================================================
//         [HttpGet]
//         public async Task<ActionResult<IEnumerable<object>>> GetAll()
//         {
//             _logger.LogInformation("Fetching all bookings");

//             try
//             {
//                 // Get all bookings
//                 var bookings = await _context.Bookings
//                     .Include(b => b.Hotel)
//                     .Include(b => b.Agency)
//                     .Include(b => b.Supplier)
//                     .OrderByDescending(b => b.Id)
//                     .ToListAsync();

//                 // Get all rooms
//                 var allRooms = await _context.BookingRooms
//                     .Include(r => r.RoomType)
//                     .ToListAsync();

//                 var result = bookings.Select(booking =>
//                 {
//                     var rooms = allRooms.Where(r => r.BookingId == booking.Id).ToList();

//                     // If no rooms found but booking says there should be rooms, log it
//                     if (rooms.Count == 0 && booking.NumberOfRooms > 0)
//                     {
//                         _logger.LogWarning("Booking {BookingId} has NumberOfRooms={NumberOfRooms} but no rooms in database",
//                             booking.Id, booking.NumberOfRooms);
//                     }

//                     return new
//                     {
//                         booking.Id,
//                         booking.BookingType,
//                         booking.BookingReference,
//                         booking.TicketNumber,
//                         HotelName = booking.Hotel?.HotelName,
//                         AgencyName = booking.Agency?.AgencyName,
//                         AgencyStaffName = booking.AgencyStaff?.Name,
//                         SupplierName = booking.Supplier?.SupplierName,
//                         booking.CheckIn,
//                         booking.CheckOut,
//                         booking.NumberOfRooms,
//                         booking.SpecialRequest,
//                         NumberOfPeople = rooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0)),
//                         booking.Status,
//                         Nights = (booking.CheckIn.HasValue && booking.CheckOut.HasValue)
//                             ? (int)(booking.CheckOut.Value - booking.CheckIn.Value).TotalDays
//                             : 0,

//                         BookingRooms = rooms.Select(r => new
//                         {
//                             r.Id,
//                             r.RoomTypeId,
//                             RoomTypeName = r.RoomType?.Name ?? "Unknown",
//                             r.Adults,
//                             r.Children,
//                             r.Inclusion,
//                             r.LeadGuestName,
//                             GuestNames = r.GuestNames ?? new List<string>(),
//                             ChildrenAges = StringToAges(r.ChildrenAges)
//                         }).ToList()
//                     };
//                 }).ToList();

//                 _logger.LogInformation("Fetched {Count} bookings", result.Count);
//                 return Ok(result);
//             }
//             catch (Exception ex)
//             {
//                 return BuildErrorResponse(ex, "Error fetching all bookings");
//             }
//         }
// [HttpGet("all-rooms")]
// public async Task<IActionResult> GetAllRooms()
// {
//     try
//     {
//         var allRooms = await _context.BookingRooms
//             .Include(r => r.Booking)
//             .Include(r => r.RoomType)
//             .Select(r => new
//             {
//                 r.Id,
//                 r.BookingId,
//                 BookingReference = r.Booking.BookingReference,
//                 RoomTypeName = r.RoomType.Name,
//                 r.Adults,
//                 r.Children,
//                 r.LeadGuestName
//             })
//             .ToListAsync();

//         return Ok(new
//         {
//             TotalRooms = allRooms.Count,
//             Rooms = allRooms
//         });
//     }
//     catch (Exception ex)
//     {
//         return BuildErrorResponse(ex, "Error fetching all rooms");
//     }
// }
 
//         // ============================================================
//         // GET: api/Booking/{id}
// [HttpGet("{id}")]
// public async Task<ActionResult<object>> GetById(int id)
// {
//     _logger.LogInformation("Fetching booking by Id: {Id}", id);

//     try
//     {
//         var booking = await _context.Bookings
//             .Include(b => b.Hotel).ThenInclude(h => h.City)
//             .Include(b => b.Hotel).ThenInclude(h => h.Country)
//             .Include(b => b.Agency)
//             .Include(b => b.Supplier)
//             .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
//             .FirstOrDefaultAsync(b => b.Id == id);

//         if (booking == null)
//         {
//             return NotFound();
//         }

//         return Ok(new
//         {
//             booking.Id,
//             booking.BookingType,
//             booking.BookingReference,
//             booking.TicketNumber,

//             // ✅ ADD THESE FOR FRONTEND
//             booking.HotelId,
//             booking.AgencyId,
//             booking.SupplierId,
//             booking.AgencyStaffId,

//             HotelName = booking.Hotel?.HotelName,
//             CityName = booking.Hotel?.City?.Name,
//             CountryName = booking.Hotel?.Country?.Name,
//             AgencyName = booking.Agency?.AgencyName,
//             AgencyStaffName = booking.AgencyStaff?.Name,
//             SupplierName = booking.Supplier?.SupplierName,

//             booking.CheckIn,
//             booking.CheckOut,
//             booking.NumberOfRooms,
//             booking.SpecialRequest,
//             NumberOfPeople = booking.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0)),
//             booking.Deadline,
//             booking.Status,

//             BookingRooms = booking.BookingRooms.Select(r => new
//             {
//                 r.Id,
//                 r.RoomTypeId,
//                 RoomTypeName = r.RoomType?.Name,
//                 r.Adults,
//                 r.Children,
//                 r.Inclusion,
//                 r.LeadGuestName,
//                 GuestNames = r.GuestNames,
//                 r.ChildrenAges
//             })
//         });
//     }
//     catch (Exception ex)
//     {
//         return BuildErrorResponse(ex, $"Error fetching booking with Id {id}");
//     }
// }


//         // ============================================================
//         // PUT: api/Booking/{id}
//         // ============================================================
//         [HttpPut("{id}")]
//         public async Task<IActionResult> Update(int id, [FromBody] BookingUpdateDto dto)
//         {
//             _logger.LogInformation("Updating booking Id: {Id}", id);

//             var existing = await _context.Bookings
//                 .Include(b => b.BookingRooms)
//                 .FirstOrDefaultAsync(b => b.Id == id);

//             if (existing == null)
//                 return NotFound();

//             try
//             {
//                 // Update main fields
//                 existing.HotelId = dto.HotelId ?? existing.HotelId;
//                 existing.AgencyId = dto.AgencyId ?? existing.AgencyId;
//                 existing.AgencyStaffId = dto.AgencyStaffId ?? existing.AgencyStaffId;
//                 existing.SupplierId = dto.SupplierId ?? existing.SupplierId;
//                 existing.CheckIn = dto.CheckIn.HasValue ? EnsureUtc(dto.CheckIn.Value) : existing.CheckIn;
//                 existing.CheckOut = dto.CheckOut.HasValue ? EnsureUtc(dto.CheckOut.Value) : existing.CheckOut;
//                 existing.SpecialRequest = dto.SpecialRequest ?? existing.SpecialRequest;

//                 if (!string.IsNullOrEmpty(dto.Status))
//                 {
//                     existing.Status = dto.Status;
//                     if (dto.Status == "Reconfirmed(Guaranteed)")
//                     {
//                         existing.Deadline = null; // clear on reconfirm
//                     }
//                 }
//                 if (dto.Deadline.HasValue)
//                 {
//                     if (existing.CheckIn.HasValue && dto.Deadline.Value >= existing.CheckIn.Value)
//                         return BadRequest(new { message = "Deadline must be before Check-In." });

//                     existing.Deadline = EnsureUtc(dto.Deadline.Value);
//                 }

//                 // Replace rooms
//                 _context.BookingRooms.RemoveRange(existing.BookingRooms);

//                 if (dto.BookingRooms != null && dto.BookingRooms.Any())
//                 {
//                     foreach (var roomDto in dto.BookingRooms)
//                     {
//                             if (!roomDto.RoomTypeId.HasValue || roomDto.RoomTypeId.Value <= 0)
//                             {
//                                 return BadRequest(new { 
//                                     message = "RoomTypeId is required and must be a valid integer."
//                                 });
//                             }
//                         _context.BookingRooms.Add(new BookingRoom
//                         {
//                             BookingId = existing.Id,
//                             RoomTypeId = roomDto.RoomTypeId.Value,  
//                             Adults = roomDto.Adults,
//                             Children = roomDto.Children,
//                             Inclusion = roomDto.Inclusion ?? string.Empty,
//                             LeadGuestName = roomDto.LeadGuestName,
//                             GuestNames = roomDto.GuestNames ?? new List<string>(),
//                             ChildrenAges = AgesToString(roomDto.ChildrenAges),
//                             CreatedAt = DateTime.UtcNow,
//                             UpdatedAt = DateTime.UtcNow
//                         });
//                     }

//                     existing.NumberOfRooms = dto.BookingRooms.Count;
//                     existing.NumberOfPeople = dto.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0));
//                 }

//                 existing.UpdatedAt = DateTime.UtcNow;

//                 await _context.SaveChangesAsync();

//                 _logger.LogInformation("Booking {Id} updated successfully", id);
//                 return Ok(new { message = "Booking updated successfully." });
//             }
//             catch (Exception ex)
//             {
//                 return BuildErrorResponse(ex, $"Failed to update booking Id {id}");
//             }
//         }

//         // ============================================================
//         // DELETE: api/Booking/{id}
//         // ============================================================
//         [HttpDelete("{id}")]
//         public async Task<IActionResult> Delete(int id)
//         {
//             _logger.LogInformation("Deleting booking Id: {Id}", id);

//             try
//             {
//                 var booking = await _context.Bookings
//                     .Include(b => b.BookingRooms)
//                     .FirstOrDefaultAsync(b => b.Id == id);

//                 if (booking == null)
//                 {
//                     _logger.LogWarning("Booking not found for deletion. Id: {Id}", id);
//                     return NotFound();
//                 }

//                 _context.BookingRooms.RemoveRange(booking.BookingRooms);
//                 _context.Bookings.Remove(booking);
//                 await _context.SaveChangesAsync();

//                 _logger.LogInformation("Booking {Id} deleted successfully", id);
//                 return Ok(new { message = "Booking deleted successfully." });
//             }
//             catch (Exception ex)
//             {
//                 return BuildErrorResponse(ex, $"Error deleting booking Id {id}");
//             }
//         }

//         // ============================================================
//         // GET: api/Booking/search?query=...
//         // ============================================================
//         [HttpGet("search")]
//         public async Task<IActionResult> Search([FromQuery] string query)
//         {
//             _logger.LogInformation("Searching bookings. Query: {Query}", query);

//             if (string.IsNullOrWhiteSpace(query))
//                 return BadRequest(new { message = "Search query cannot be empty." });

//             query = query.ToLower();

//             try
//             {
//                 var results = await _context.Bookings
//                     .Include(b => b.Hotel)
//                     .Include(b => b.Agency)
//                     .Where(b =>
//                         (b.Hotel != null && b.Hotel.HotelName.ToLower().Contains(query)) ||
//                         (b.Agency != null && b.Agency.AgencyName.ToLower().Contains(query)) ||
//                         (!string.IsNullOrEmpty(b.TicketNumber) && b.TicketNumber.ToLower().Contains(query)) ||
//                         (!string.IsNullOrEmpty(b.BookingReference) && b.BookingReference.ToLower().Contains(query)))
//                     .OrderByDescending(b => b.Id)
//                     .Select(b => new
//                     {
//                         b.Id,
//                         b.BookingType,
//                         b.BookingReference,
//                         b.TicketNumber,
//                         HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
//                         AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
//                         b.Status,
//                         b.CheckIn,
//                         b.CheckOut
//                     })
//                     .ToListAsync();

//                 _logger.LogInformation("Search found {Count} bookings", results.Count);
//                 return Ok(results);
//             }
//             catch (Exception ex)
//             {
//                 return BuildErrorResponse(ex, "Error searching bookings");
//             }
//         }

//         // ============================================================
//         // GET: api/Booking/paged?page=1&pageSize=10
//         // ============================================================
//         [HttpGet("paged")]
//         public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//         {
//             _logger.LogInformation("Getting paged bookings. Page: {Page}, Size: {Size}", page, pageSize);

//             if (page < 1 || pageSize < 1)
//                 return BadRequest(new { message = "Invalid pagination parameters." });

//             try
//             {
//                 var total = await _context.Bookings.CountAsync();

//                 var results = await _context.Bookings
//                     .Include(b => b.Hotel)
//                     .Include(b => b.Agency)
//                     .OrderByDescending(b => b.Id)
//                     .Skip((page - 1) * pageSize)
//                     .Take(pageSize)
//                     .Select(b => new
//                     {
//                         b.Id,
//                         b.BookingType,
//                         b.BookingReference,
//                         b.TicketNumber,
//                         HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
//                         AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
//                         b.Status,
//                         b.CheckIn,
//                         b.CheckOut
//                     })
//                     .ToListAsync();

//                 return Ok(new { total, page, pageSize, results });
//             }
//             catch (Exception ex)
//             {
//                 return BuildErrorResponse(ex, "Error fetching paged bookings");
//             }
//         }

//         // ============================================================
//         // GET: api/Booking/hotels-autocomplete?query=...
//         // ============================================================
//         [HttpGet("hotels-autocomplete")]
//         public async Task<IActionResult> HotelsAutocomplete([FromQuery] string query)
//         {
//             _logger.LogInformation("Hotels autocomplete. Query: {Query}", query);

//             if (string.IsNullOrWhiteSpace(query))
//                 return BadRequest(new { message = "Query cannot be empty." });

//             query = query.ToLower();

//             try
//             {
//                 var hotels = await _context.HotelInfo
//                     .Include(h => h.City)
//                     .Include(h => h.Country)
//                     .Where(h =>
//                         h.HotelName.ToLower().Contains(query) ||
//                         h.Address.ToLower().Contains(query) ||
//                         (h.City != null && h.City.Name.ToLower().Contains(query)) ||
//                         (h.Country != null && h.Country.Name.ToLower().Contains(query))
//                     )
//                     .OrderBy(h => h.HotelName)
//                     .Select(h => new
//                     {
//                         h.Id,
//                         h.HotelName,
//                         CityName = h.City != null ? h.City.Name : null,
//                         CountryName = h.Country != null ? h.Country.Name : null
//                     })
//                     .Take(10)
//                     .ToListAsync();

//                 _logger.LogInformation("Hotels autocomplete returned {Count} results", hotels.Count);
//                 return Ok(hotels);
//             }
//             catch (Exception ex)
//             {
//                 return BuildErrorResponse(ex, "Error in hotels autocomplete");
//             }
//         }

//         // ============================================================
//         // POST: api/Booking/create-with-commercial
//         // ============================================================
//         [HttpPost("create-with-commercial")]
//         public async Task<IActionResult> CreateWithCommercial([FromBody] BookingCommercialDTO dto)
//         {
//             _logger.LogInformation("Creating booking with commercial payload");

//             if (dto == null || dto.Booking == null)
//                 return BadRequest(new { message = "Invalid request payload." });

//             await using var transaction = await _context.Database.BeginTransactionAsync();

//             try
//             {
//                 // 1) Save Commercial first (if provided)
//                 Commercial? commercial = null;
//                 if (dto.Commercial != null)
//                 {
//                     _context.Commercials.Add(dto.Commercial);
//                     await _context.SaveChangesAsync();
//                     commercial = dto.Commercial;
//                 }

//                 // 2) Prepare Booking
//                 var booking = dto.Booking;

//                 if (booking.CheckIn.HasValue)
//                     booking.CheckIn = EnsureUtc(booking.CheckIn.Value);
//                 if (booking.CheckOut.HasValue)
//                     booking.CheckOut = EnsureUtc(booking.CheckOut.Value);

//                 // Auto booking type/ref/ticket here as well (mirrors Create)
//                 string bType = DetectBookingType(booking.HotelId, booking.SupplierId);
//                 booking.BookingType = bType;
//                 booking.BookingReference = await _context.GenerateBookingReferenceAsync(bType);
//                 if (string.IsNullOrWhiteSpace(booking.BookingReference))
//                     return StatusCode(500, new { message = "Failed to generate BookingReference." });

//                 booking.TicketNumber = $"BOOKING-{DateTime.UtcNow:yyyyMMddHHmm}-{booking.BookingReference}";
//                 booking.Status = string.IsNullOrEmpty(booking.Status) ? "Confirmed" : booking.Status;

//                 // 3) Link Commercial if exists
//                 if (commercial != null)
//                     booking.CommercialId = commercial.Id;

//                 _context.Bookings.Add(booking);
//                 await _context.SaveChangesAsync();

//                 // 4) Commit
//                 await transaction.CommitAsync();

//                 _logger.LogInformation("Booking-with-commercial created. BookingId: {Id}, CommercialId: {CommercialId}", booking.Id, commercial?.Id);

//                 return Ok(new
//                 {
//                     message = "Booking and Commercial saved successfully.",
//                     bookingId = booking.Id,
//                     commercialId = commercial?.Id,
//                     ticket = booking.TicketNumber
//                 });
//             }
//             catch (Exception ex)
//             {
//                 await transaction.RollbackAsync();
//                 return BuildErrorResponse(ex, "Error saving booking with commercial data");
//             }
//         }

//         // ============================================================
//         // GET: api/Booking/pending-reconfirmations
//         // ============================================================
//         [HttpGet("pending-reconfirmations")]
//         public async Task<IActionResult> GetPendingReconfirmations()
//         {
//             _logger.LogInformation("Fetching pending reconfirmations");

//             try
//             {
//                 var pending = await _context.Bookings
//                     .Include(b => b.Hotel)
//                     .Include(b => b.Agency)
//                     .Where(b => b.Status == "Confirmed" && b.Deadline.HasValue)
//                     .OrderBy(b => b.Deadline)
//                     .Select(b => new
//                     {
//                         b.Id,
//                         b.BookingType,
//                         b.BookingReference,
//                         b.TicketNumber,
//                         HotelName = b.Hotel != null ? b.Hotel.HotelName : null,
//                         AgencyName = b.Agency != null ? b.Agency.AgencyName : null,
//                         b.Deadline
//                     })
//                     .ToListAsync();

//                 _logger.LogInformation("Pending reconfirmations count: {Count}", pending.Count);
//                 return Ok(pending);
//             }
//             catch (Exception ex)
//             {
//                 return BuildErrorResponse(ex, "Error fetching pending reconfirmations");
//             }
//         }

//         // ============================================================
//         // Centralized error response & logging
//         // ============================================================
//         private ObjectResult BuildErrorResponse(Exception ex, string context)
//         {
//             string inner1 = ex.InnerException?.Message ?? "null";
//             string inner2 = ex.InnerException?.InnerException?.Message ?? "null";
//             string stack = ex.StackTrace ?? "null";

//             _logger.LogError(ex,
//                 "❌ {Context}\n" +
//                 "Error: {Error}\n" +
//                 "Inner1: {Inner1}\n" +
//                 "Inner2: {Inner2}\n" +
//                 "Source: {Source}\n" +
//                 "TargetMethod: {Target}\n" +
//                 "Stack: {Stack}",
//                 context,
//                 ex.Message,
//                 inner1,
//                 inner2,
//                 ex.Source,
//                 ex.TargetSite?.Name,
//                 stack
//             );

//             return StatusCode(500, new
//             {
//                 message = context,
//                 error = ex.Message,
//                 inner1,
//                 inner2,
//                 type = ex.GetType().FullName,
//                 source = ex.Source,
//                 target = ex.TargetSite?.Name,
//                 stackTrace = stack
//             });
//         }

//         [HttpPatch("Booking/{id}/status")]
//         public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateBookingStatusRequest req)
//         {
//             var booking = await _context.Bookings.FindAsync(id);
//             if (booking == null) return NotFound();

//             booking.Status = req.Status;
//             booking.AgentVoucher = req.AgentVoucher;
            
//             booking.CancelReason = req.CancelReason;

//             await _context.SaveChangesAsync();
//             return Ok(booking);
//         }

//     }
// }
