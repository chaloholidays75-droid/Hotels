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

        public BookingRoomsManagementController(AppDbContext context)
        {
            _context = context;
        }

        // ------------------------
        // GET: api/booktype/rooms/{bookingId}
        // ------------------------
        [HttpGet("rooms/{bookingId}")]
        public async Task<ActionResult<IEnumerable<BookingRoomDto>>> GetBookingRooms(int bookingId)
        {
            var rooms = await _context.BookingRooms
                .Where(br => br.BookingId == bookingId)
                .ToListAsync();

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

            return Ok(roomDtos);
        }

        // ------------------------
        // GET: api/booktype/roomtypes/{hotelId}
        // ------------------------
        [HttpGet("roomtypes/{hotelId}")]
        public async Task<ActionResult<IEnumerable<RoomType>>> GetRoomTypes(int hotelId)
        {
            var types = await _context.RoomTypes
                .Where(rt => rt.HotelId == hotelId && rt.IsActive)
                .ToListAsync();

            return Ok(types);
        }

        // ------------------------
        // POST: api/booktype/room
        // ------------------------
        [HttpPost("room")]
        public async Task<ActionResult<BookingRoomDto>> CreateBookingRoom([FromBody] BookingRoomDto dto)
        {
            var booking = await _context.Bookings.FindAsync(dto.Id);
            if (booking == null)
                return BadRequest(new { message = "Booking not found" });

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

        // ------------------------
        // PUT: api/booktype/room/{id}
        // ------------------------
        [HttpPut("room/{id}")]
        public async Task<ActionResult<BookingRoomDto>> UpdateBookingRoom(int id, [FromBody] BookingRoomDto dto)
        {
            var room = await _context.BookingRooms.FindAsync(id);
            if (room == null) return NotFound();

          
            room.RoomTypeId = dto.RoomTypeId;
            room.Adults = dto.Adults;
            room.Children = dto.Children;
            room.ChildrenAges = dto.ChildrenAges != null ? string.Join(',', dto.ChildrenAges) : null;
            room.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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

        // ------------------------
        // DELETE: api/booktype/room/{id}
        // ------------------------
        [HttpDelete("room/{id}")]
        public async Task<IActionResult> DeleteBookingRoom(int id)
        {
            var room = await _context.BookingRooms
                .Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            _context.BookingRooms.Remove(room);
            await _context.SaveChangesAsync();

            // Recalculate total number of people
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.Id == room.BookingId);

            if (booking != null)
            {
                booking.NumberOfPeople = booking.BookingRooms.Sum(r => (r.Adults ?? 0) + (r.Children ?? 0));
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Deleted successfully" });
        }

        // ------------------------
        // RoomType endpoints
        // ------------------------
        [HttpPost("roomtype")]
        public async Task<ActionResult<RoomType>> CreateRoomType([FromBody] RoomType roomType)
        {
            roomType.IsActive = true;
            _context.RoomTypes.Add(roomType);
            await _context.SaveChangesAsync();
            return Ok(roomType);
        }

        [HttpPut("roomtype/{id}")]
        public async Task<ActionResult<RoomType>> UpdateRoomType(int id, [FromBody] RoomType roomType)
        {
            var existing = await _context.RoomTypes.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = roomType.Name;
            existing.IsActive = roomType.IsActive;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("roomtype/{id}")]
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            var roomType = await _context.RoomTypes.FindAsync(id);
            if (roomType == null) return NotFound();

            _context.RoomTypes.Remove(roomType);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted successfully" });
        }

        [HttpGet("roomtypes/autocomplete")]
        public async Task<ActionResult<IEnumerable<RoomType>>> AutocompleteRoomTypes(
            [FromQuery] int hotelId,
            [FromQuery] string query)
        {
            query ??= string.Empty;

            var types = await _context.RoomTypes
                .Where(rt => rt.HotelId == hotelId && rt.IsActive && rt.Name.ToLowerInvariant().Contains(query.ToLowerInvariant()))
                .OrderBy(rt => rt.Name)
                .ToListAsync();

            return Ok(types);
        }
    }
}
