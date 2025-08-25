using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Models;

namespace TicketingSystem.Controllers
{
    [ApiController]
    [Route("api/hotelsales")]
    public class HotelSalesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HotelSalesController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ CREATE (POST)
        [HttpPost]
        public async Task<IActionResult> CreateHotelSale([FromBody] HotelSale hotelSale)
        {
            _context.HotelSales.Add(hotelSale);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetHotelSaleById), new { id = hotelSale.Id }, hotelSale);
        }

        // ✅ READ (GET all)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.HotelSales.ToListAsync());
        }

        // ✅ READ (GET by Id)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHotelSaleById(int id)
        {
            var sale = await _context.HotelSales.FindAsync(id);
            if (sale == null) return NotFound();
            return Ok(sale);
        }

        // ✅ UPDATE (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHotelSale(int id, [FromBody] HotelSale hotelSale)
        {
            if (id != hotelSale.Id)
                return BadRequest("Id mismatch");

            var existingSale = await _context.HotelSales.FindAsync(id);
            if (existingSale == null)
                return NotFound();

            _context.Entry(existingSale).CurrentValues.SetValues(hotelSale);
            await _context.SaveChangesAsync();

            return Ok(hotelSale);
        }

        // ✅ DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHotelSale(int id)
        {
            var sale = await _context.HotelSales.FindAsync(id);
            if (sale == null)
                return NotFound();

            _context.HotelSales.Remove(sale);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
