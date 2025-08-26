using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;

namespace TicketingSystem.Controllers
{
    [ApiController]
    [Route("api/hotelinfo")]
    public class HotelInfosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HotelInfosController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ CREATE (POST)
        [HttpPost]
        public async Task<IActionResult> CreatehotelInfo([FromBody] HotelInfo hotelInfo)
        {
            _context.HotelInfos.Add(hotelInfo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GethotelInfoById), new { id = hotelInfo.Id }, hotelInfo);
        }

        // ✅ READ (GET all)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.HotelInfos.ToListAsync());
        }

        // ✅ READ (GET by Id)
        [HttpGet("{id}")]
        public async Task<IActionResult> GethotelInfoById(int id)
        {
            var sale = await _context.HotelInfos.FindAsync(id);
            if (sale == null) return NotFound();
            return Ok(sale);
        }

        // ✅ UPDATE (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatehotelInfo(int id, [FromBody] HotelInfo hotelInfo)
        {
            if (id != hotelInfo.Id)
                return BadRequest("Id mismatch");

            var existingSale = await _context.HotelInfos.FindAsync(id);
            if (existingSale == null)
                return NotFound();

            _context.Entry(existingSale).CurrentValues.SetValues(hotelInfo);
            await _context.SaveChangesAsync();

            return Ok(hotelInfo);
        }

        // ✅ DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletehotelInfo(int id)
        {
            var sale = await _context.HotelInfos.FindAsync(id);
            if (sale == null)
                return NotFound();

            _context.HotelInfos.Remove(sale);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
