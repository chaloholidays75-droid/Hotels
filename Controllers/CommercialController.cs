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
    public class CommercialController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CommercialController> _logger;

        public CommercialController(AppDbContext context, ILogger<CommercialController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // -------------------- CREATE --------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CommercialCreateDto dto)
        {
            if (dto == null || dto.BookingId <= 0)
                return BadRequest("Invalid payload. BookingId is required.");

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == dto.BookingId);
            if (booking == null)
                return NotFound("Booking not found.");

            var entity = new Commercial
            {
                BookingId = dto.BookingId,
                BuyingCurrency = dto.BuyingCurrency,
                BuyingAmount = dto.BuyingAmount,
                Commissionable = dto.Commissionable,
                CommissionType = dto.CommissionType,
                CommissionValue = dto.CommissionValue,
                BuyingVatIncluded = dto.BuyingVatIncluded,
                BuyingVatPercent = dto.BuyingVatPercent,
                AdditionalCostsJson = dto.AdditionalCostsJson,
                SellingCurrency = dto.SellingCurrency,
                SellingPrice = dto.SellingPrice,
                Incentive = dto.Incentive,
                IncentiveType = dto.IncentiveType,
                IncentiveValue = dto.IncentiveValue,
                SellingVatIncluded = dto.SellingVatIncluded,
                SellingVatPercent = dto.SellingVatPercent,
                DiscountsJson = dto.DiscountsJson,
                ExchangeRate = dto.ExchangeRate,
                AutoCalculateRate = dto.AutoCalculateRate,
                GrossBuying = dto.GrossBuying,
                NetBuying = dto.NetBuying,
                GrossSelling = dto.GrossSelling,
                NetSelling = dto.NetSelling,
                Profit = dto.Profit,
                ProfitMarginPercent = dto.ProfitMarginPercent,
                MarkupPercent = dto.MarkupPercent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Commercials.Add(entity);
            await _context.SaveChangesAsync();

            // ✅ Link to Booking.CommercialId
            booking.CommercialId = entity.Id;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commercial created successfully and linked with booking", entity.Id });
        }

        // -------------------- READ ALL --------------------
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest("Invalid pagination parameters.");

            var data = await _context.Commercials
                .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                .OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.BookingId,
                    c.BuyingCurrency,
                    c.SellingCurrency,
                    c.Profit,
                    c.ProfitMarginPercent,
                    HotelName = c.Booking.Hotel.HotelName,
                    TicketNumber = c.Booking.TicketNumber,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            var total = await _context.Commercials.CountAsync();
            return Ok(new { total, page, pageSize, data });
        }

        // -------------------- READ SINGLE --------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _context.Commercials
                .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (data == null)
                return NotFound("Commercial not found.");

            return Ok(data);
        }

        // -------------------- READ BY BOOKING --------------------
        [HttpGet("by-booking/{bookingId}")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            var data = await _context.Commercials
                .Include(c => c.Booking)
                .FirstOrDefaultAsync(c => c.BookingId == bookingId);

            if (data == null)
                return NotFound("No commercial found for this booking.");

            return Ok(data);
        }

// -------------------- UPDATE --------------------
[HttpPut("{id}")]
public async Task<IActionResult> Update(int id, [FromBody] CommercialUpdateDto dto)
{
    var existing = await _context.Commercials
        .Include(c => c.Booking)
        .FirstOrDefaultAsync(c => c.Id == id);
    if (existing == null)
        return NotFound("Commercial not found.");

    // update values...
    existing.BuyingCurrency = dto.BuyingCurrency ?? existing.BuyingCurrency;
    existing.SellingCurrency = dto.SellingCurrency ?? existing.SellingCurrency;
    existing.BuyingAmount = dto.BuyingAmount ?? existing.BuyingAmount;
    existing.SellingPrice = dto.SellingPrice ?? existing.SellingPrice;
    existing.Commissionable = dto.Commissionable ?? existing.Commissionable;
    existing.CommissionType = dto.CommissionType ?? existing.CommissionType;
    existing.CommissionValue = dto.CommissionValue ?? existing.CommissionValue;
    existing.Incentive = dto.Incentive ?? existing.Incentive;
    existing.IncentiveType = dto.IncentiveType ?? existing.IncentiveType;
    existing.IncentiveValue = dto.IncentiveValue ?? existing.IncentiveValue;
    existing.ExchangeRate = dto.ExchangeRate ?? existing.ExchangeRate;
    existing.Profit = dto.Profit ?? existing.Profit;
    existing.ProfitMarginPercent = dto.ProfitMarginPercent ?? existing.ProfitMarginPercent;
    existing.MarkupPercent = dto.MarkupPercent ?? existing.MarkupPercent;
    existing.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    // ✅ Ensure booking links to this commercial
    if (existing.Booking != null)
    {
        existing.Booking.CommercialId = existing.Id;
        await _context.SaveChangesAsync();
    }

    return Ok(new { message = "Commercial updated and linked successfully", id = existing.Id });
}


        // -------------------- DELETE --------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.Commercials.FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null)
                return NotFound("Commercial not found.");

            // ✅ Unlink booking if exists
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.CommercialId == id);
            if (booking != null)
                booking.CommercialId = null;

            _context.Commercials.Remove(existing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commercial deleted and unlinked successfully" });
        }

        // -------------------- SEARCH --------------------
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query is required.");

            query = query.ToLower();

            var results = await _context.Commercials
                .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                .Where(c =>
                    c.Booking.TicketNumber.ToLower().Contains(query) ||
                    c.Booking.Hotel.HotelName.ToLower().Contains(query)
                )
                .Select(c => new
                {
                    c.Id,
                    c.BookingId,
                    c.BuyingCurrency,
                    c.SellingCurrency,
                    c.Profit,
                    c.ProfitMarginPercent,
                    Ticket = c.Booking.TicketNumber,
                    Hotel = c.Booking.Hotel.HotelName
                })
                .ToListAsync();

            return Ok(results);
        }

        // -------------------- BOOKING DROPDOWN --------------------
        [HttpGet("bookings-dropdown")]
        public async Task<IActionResult> GetBookingsForDropdown()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Agency)
                .OrderByDescending(b => b.Id)
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    Hotel = b.Hotel.HotelName,
                    Agency = b.Agency.AgencyName,
                    b.CommercialId
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // -------------------- ANALYTICS SUMMARY --------------------
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var total = await _context.Commercials.CountAsync();
            var totalProfit = await _context.Commercials.SumAsync(c => (decimal?)c.Profit) ?? 0;
            var avgMargin = await _context.Commercials.AverageAsync(c => (decimal?)c.ProfitMarginPercent) ?? 0;

            return Ok(new
            {
                total,
                totalProfit,
                avgMargin,
                lastUpdated = DateTime.UtcNow
            });
        }
        // -------------------- LINK COMMERCIAL TO BOOKING --------------------
[HttpPut("link/{bookingId}/{commercialId}")]
public async Task<IActionResult> LinkCommercialToBooking(int bookingId, int commercialId)
{
    var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
    if (booking == null)
        return NotFound($"Booking with ID {bookingId} not found.");

    var commercial = await _context.Commercials.FirstOrDefaultAsync(c => c.Id == commercialId);
    if (commercial == null)
        return NotFound($"Commercial with ID {commercialId} not found.");

    // ✅ Link them
    booking.CommercialId = commercialId;
    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = $"Booking {bookingId} successfully linked with Commercial {commercialId}.",
        booking.Id,
        booking.CommercialId
    });
}

    }
}
