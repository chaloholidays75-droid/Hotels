using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/booktype")]
    public class BookingRoomsManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BookingRoomsManagementController> _logger;

        public BookingRoomsManagementController(AppDbContext context, ILogger<BookingRoomsManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ------------------------
        // GET: api/booktype/rooms/{bookingId}
        // ------------------------
        [HttpGet("rooms/{bookingId}")]
        public async Task<ActionResult<IEnumerable<BookingRoomDto>>> GetBookingRooms(int bookingId)
        {
            _logger.LogInformation("Fetching booking rooms for BookingId: {BookingId}", bookingId);

            try
            {
                var rooms = await _context.BookingRooms
                    .Where(br => br.BookingId == bookingId)
                    .ToListAsync();

                if (!rooms.Any())
                {
                    _logger.LogWarning("No rooms found for BookingId: {BookingId}", bookingId);
                }

                var roomDtos = rooms.Select(br => new BookingRoomDto
                {
                    Id = br.Id,
                    RoomTypeId = br.RoomTypeId,
                    Adults = br.Adults ?? 0,
                    Children = br.Children ?? 0,
                    ChildrenAges = !string.IsNullOrEmpty(br.ChildrenAges)
                        ? br.ChildrenAges.Split(',')
                            .Select(s => int.TryParse(s, out var age) ? age : 0)
                            .ToList()
                        : new List<int>()
                }).ToList();

                _logger.LogInformation("Successfully fetched {Count} rooms for BookingId {BookingId}", roomDtos.Count, bookingId);
                return Ok(roomDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching booking rooms for BookingId {BookingId}", bookingId);
                return StatusCode(500, "An error occurred while fetching booking rooms.");
            }
        }

        // ------------------------
        // GET: api/booktype/roomtypes/{hotelId}
        // ------------------------
        [HttpGet("roomtypes/{hotelId}")]
        public async Task<ActionResult<IEnumerable<RoomType>>> GetRoomTypes(int hotelId)
        {
            _logger.LogInformation("Fetching room types for HotelId: {HotelId}", hotelId);

            try
            {
                var types = await _context.RoomTypes
                    .Where(rt => rt.HotelId == hotelId && rt.IsActive)
                    .ToListAsync();

                if (!types.Any())
                {
                    _logger.LogWarning("No room types found for HotelId: {HotelId}", hotelId);
                }

                _logger.LogInformation("Fetched {Count} room types for HotelId: {HotelId}", types.Count, hotelId);
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching room types for HotelId {HotelId}", hotelId);
                return StatusCode(500, "An error occurred while fetching room types.");
            }
        }

        // ------------------------
        // POST: api/booktype/room
        // ------------------------
        [HttpPost("room")]
        public async Task<ActionResult<BookingRoomDto>> CreateBookingRoom([FromBody] BookingRoomDto dto)
        {
            _logger.LogInformation("Creating new booking room for BookingId {BookingId}", dto.Id);

            try
            {
                var booking = await _context.Bookings.FindAsync(dto.Id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found for Id {BookingId}", dto.Id);
                    return BadRequest(new { message = "Booking not found" });
                }

                var room = new BookingRoom
                {
                    BookingId = dto.Id,
                    RoomTypeId = dto.RoomTypeId,
                    Adults = dto.Adults,
                    Children = dto.Children,
                    ChildrenAges = dto.ChildrenAges != null ? string.Join(',', dto.ChildrenAges) : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BookingRooms.Add(room);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created booking room {RoomId} for BookingId {BookingId}", room.Id, dto.Id);

                var roomDto = new BookingRoomDto
                {
                    Id = room.Id,
                    RoomTypeId = room.RoomTypeId,
                    Adults = room.Adults ?? 0,
                    Children = room.Children ?? 0,
                    ChildrenAges = !string.IsNullOrEmpty(room.ChildrenAges)
                        ? room.ChildrenAges.Split(',')
                            .Select(s => int.TryParse(s, out var age) ? age : 0)
                            .ToList()
                        : new List<int>()
                };

                return Ok(roomDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating booking room for BookingId {BookingId}", dto.Id);
                return StatusCode(500, "An error occurred while creating the booking room.");
            }
        }

        // ------------------------
        // PUT: api/booktype/room/{id}
        // ------------------------
        [HttpPut("room/{id}")]
        public async Task<ActionResult<BookingRoomDto>> UpdateBookingRoom(int id, [FromBody] BookingRoomDto dto)
        {
            _logger.LogInformation("Updating booking room {RoomId}", id);

            try
            {
                var room = await _context.BookingRooms.FindAsync(id);
                if (room == null)
                {
                    _logger.LogWarning("Room not found for Id {RoomId}", id);
                    return NotFound();
                }

                room.RoomTypeId = dto.RoomTypeId;
                room.Adults = dto.Adults;
                room.Children = dto.Children;
                room.ChildrenAges = dto.ChildrenAges != null ? string.Join(',', dto.ChildrenAges) : null;
                room.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated booking room {RoomId} successfully", id);

                var roomDto = new BookingRoomDto
                {
                    Id = room.Id,
                    RoomTypeId = room.RoomTypeId,
                    Adults = room.Adults ?? 0,
                    Children = room.Children ?? 0,
                    ChildrenAges = !string.IsNullOrEmpty(room.ChildrenAges)
                        ? room.ChildrenAges.Split(',')
                            .Select(s => int.TryParse(s, out var age) ? age : 0)
                            .ToList()
                        : new List<int>()
                };

                return Ok(roomDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking room {RoomId}", id);
                return StatusCode(500, "An error occurred while updating the booking room.");
            }
        }

        // ------------------------
        // DELETE: api/booktype/room/{id}
        // ------------------------
        [HttpDelete("room/{id}")]
        public async Task<IActionResult> DeleteBookingRoom(int id)
        {
            _logger.LogInformation("Deleting booking room {RoomId}", id);

            try
            {
                var room = await _context.BookingRooms
                    .Include(r => r.Booking)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (room == null)
                {
                    _logger.LogWarning("Room not found for deletion. Id: {RoomId}", id);
                    return NotFound();
                }

                _context.BookingRooms.Remove(room);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted booking room {RoomId}. Updating booking people count.", id);

                var booking = await _context.Bookings
                    .Include(b => b.BookingRooms)
                    .FirstOrDefaultAsync(b => b.Id == room.BookingId);

                if (booking != null)
                {
                    booking.NumberOfPeople = booking.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0));
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully deleted booking room {RoomId}", id);
                return Ok(new { message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting booking room {RoomId}", id);
                return StatusCode(500, "An error occurred while deleting the booking room.");
            }
        }

        // ------------------------
        // RoomType endpoints
        // ------------------------
        [HttpPost("roomtype")]
        public async Task<ActionResult<RoomType>> CreateRoomType([FromBody] RoomType roomType)
        {
            _logger.LogInformation("Creating new room type for HotelId {HotelId}", roomType.HotelId);

            try
            {
                roomType.IsActive = true;
                _context.RoomTypes.Add(roomType);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created room type {RoomTypeId} for HotelId {HotelId}", roomType.Id, roomType.HotelId);
                return Ok(roomType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating room type for HotelId {HotelId}", roomType.HotelId);
                return StatusCode(500, "An error occurred while creating the room type.");
            }
        }

        [HttpPut("roomtype/{id}")]
        public async Task<ActionResult<RoomType>> UpdateRoomType(int id, [FromBody] RoomType roomType)
        {
            _logger.LogInformation("Updating room type {RoomTypeId}", id);

            try
            {
                var existing = await _context.RoomTypes.FindAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Room type not found for Id {RoomTypeId}", id);
                    return NotFound();
                }

                existing.Name = roomType.Name;
                existing.IsActive = roomType.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated room type {RoomTypeId} successfully", id);
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room type {RoomTypeId}", id);
                return StatusCode(500, "An error occurred while updating the room type.");
            }
        }

        [HttpDelete("roomtype/{id}")]
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            _logger.LogInformation("Deleting room type {RoomTypeId}", id);

            try
            {
                var roomType = await _context.RoomTypes.FindAsync(id);
                if (roomType == null)
                {
                    _logger.LogWarning("Room type not found for deletion. Id: {RoomTypeId}", id);
                    return NotFound();
                }

                _context.RoomTypes.Remove(roomType);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted room type {RoomTypeId} successfully", id);
                return Ok(new { message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room type {RoomTypeId}", id);
                return StatusCode(500, "An error occurred while deleting the room type.");
            }
        }

        [HttpGet("roomtypes/autocomplete")]
        public async Task<ActionResult<IEnumerable<RoomType>>> AutocompleteRoomTypes(
            [FromQuery] int hotelId,
            [FromQuery] string query)
        {
            _logger.LogInformation("Autocomplete search for RoomTypes. HotelId: {HotelId}, Query: {Query}", hotelId, query);

            try
            {
                query ??= string.Empty;

                var types = await _context.RoomTypes
                    .Where(rt => rt.HotelId == hotelId && rt.IsActive && rt.Name.ToLowerInvariant().Contains(query.ToLowerInvariant()))
                    .OrderBy(rt => rt.Name)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} room types matching '{Query}' for HotelId {HotelId}", types.Count, query, hotelId);
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching autocomplete room types for HotelId {HotelId}", hotelId);
                return StatusCode(500, "An error occurred while fetching autocomplete room types.");
            }
        }
    }
}
