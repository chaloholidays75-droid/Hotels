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
    [Route("api/commercial")]
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
            _logger.LogInformation("📦 [POST] /api/commercial called with BookingId={BookingId}", dto?.BookingId);

            try
            {
                if (dto == null || dto.BookingId <= 0)
                {
                    _logger.LogWarning("❌ Invalid payload or missing BookingId.");
                    return BadRequest("Invalid payload. BookingId is required.");
                }

                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == dto.BookingId);
                if (booking == null)
                {
                    _logger.LogWarning("⚠️ BookingId {BookingId} not found.", dto.BookingId);
                    return NotFound("Booking not found.");
                }

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

                booking.CommercialId = entity.Id;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Commercial ID {CommercialId} created for BookingId {BookingId}.", entity.Id, dto.BookingId);
                return Ok(new { message = "Commercial created successfully", entity.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating commercial for BookingId {BookingId}", dto?.BookingId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- READ ALL --------------------
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation("📄 [GET] /api/commercial called page={Page}, pageSize={PageSize}", page, pageSize);

            try
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
                _logger.LogInformation("✅ Returned {Count} records (Total={Total})", data.Count, total);

                return Ok(new { total, page, pageSize, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error fetching all commercials.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- READ SINGLE --------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("🔍 [GET] /api/commercial/{Id} called", id);

            try
            {
                var data = await _context.Commercials
                    .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (data == null)
                {
                    _logger.LogWarning("⚠️ Commercial ID {Id} not found.", id);
                    return NotFound("Commercial not found.");
                }

                _logger.LogInformation("✅ Commercial ID {Id} found.", id);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error fetching Commercial ID {Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- READ BY BOOKING --------------------
        [HttpGet("by-booking/{bookingId}")]
        public async Task<IActionResult> GetByBooking(int bookingId)
        {
            _logger.LogInformation("🔎 [GET] /api/commercial/by-booking/{BookingId} called", bookingId);

            try
            {
                if (bookingId <= 0)
                {
                    _logger.LogWarning("❌ Invalid BookingId {BookingId}", bookingId);
                    return BadRequest("Invalid BookingId.");
                }

                var bookingExists = await _context.Bookings.AnyAsync(b => b.Id == bookingId);
                if (!bookingExists)
                {
                    _logger.LogWarning("⚠️ BookingId {BookingId} not found.", bookingId);
                    return NotFound($"Booking ID {bookingId} not found.");
                }

                var data = await _context.Commercials
                    .Include(c => c.Booking)
                    .FirstOrDefaultAsync(c => c.BookingId == bookingId);

                if (data == null)
                {
                    _logger.LogWarning("📭 No commercial found for BookingId {BookingId}", bookingId);
                    return NotFound($"No commercial found for booking ID {bookingId}.");
                }

                _logger.LogInformation("✅ Found Commercial ID {CommercialId} for BookingId {BookingId}.", data.Id, bookingId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception while fetching commercial by bookingId {BookingId}", bookingId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- UPDATE --------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CommercialUpdateDto dto)
        {
            _logger.LogInformation("📝 [PUT] /api/commercial/{Id} called", id);

            try
            {
                var existing = await _context.Commercials
                    .Include(c => c.Booking)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (existing == null)
                {
                    _logger.LogWarning("⚠️ Commercial ID {Id} not found.", id);
                    return NotFound("Commercial not found.");
                }

                // Update values
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

                if (existing.Booking != null)
                {
                    existing.Booking.CommercialId = existing.Id;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("✅ Commercial ID {Id} updated successfully.", id);
                return Ok(new { message = "Commercial updated successfully", id = existing.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error updating Commercial ID {Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- DELETE --------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("🗑️ [DELETE] /api/commercial/{Id} called", id);

            try
            {
                var existing = await _context.Commercials.FirstOrDefaultAsync(c => c.Id == id);
                if (existing == null)
                {
                    _logger.LogWarning("⚠️ Commercial ID {Id} not found.", id);
                    return NotFound("Commercial not found.");
                }

                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.CommercialId == id);
                if (booking != null)
                    booking.CommercialId = null;

                _context.Commercials.Remove(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Commercial ID {Id} deleted.", id);
                return Ok(new { message = "Commercial deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error deleting Commercial ID {Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- SEARCH --------------------
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            _logger.LogInformation("🔍 [GET] /api/commercial/search?query={Query} called", query);

            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Search query is required.");

                query = query.ToLower();

                var results = await _context.Commercials
                    .Include(c => c.Booking).ThenInclude(b => b.Hotel)
                    .Where(c =>
                        c.Booking.TicketNumber.ToLower().Contains(query) ||
                        c.Booking.Hotel.HotelName.ToLower().Contains(query))
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

                _logger.LogInformation("✅ Search returned {Count} results for query '{Query}'", results.Count, query);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error searching Commercials with query '{Query}'", query);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- BOOKING DROPDOWN --------------------
        [HttpGet("bookings-dropdown")]
        public async Task<IActionResult> GetBookingsForDropdown()
        {
            _logger.LogInformation("📋 [GET] /api/commercial/bookings-dropdown called");

            try
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

                _logger.LogInformation("✅ Returned {Count} bookings for dropdown", bookings.Count);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error fetching bookings for dropdown");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- ANALYTICS SUMMARY --------------------
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            _logger.LogInformation("📊 [GET] /api/commercial/summary called");

            try
            {
                var total = await _context.Commercials.CountAsync();
                var totalProfit = await _context.Commercials.SumAsync(c => (decimal?)c.Profit) ?? 0;
                var avgMargin = await _context.Commercials.AverageAsync(c => (decimal?)c.ProfitMarginPercent) ?? 0;

                _logger.LogInformation("✅ Summary calculated: total={Total}, profit={Profit}, avgMargin={Avg}", total, totalProfit, avgMargin);

                return Ok(new
                {
                    total,
                    totalProfit,
                    avgMargin,
                    lastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error generating summary");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // -------------------- LINK COMMERCIAL TO BOOKING --------------------
        [HttpPut("link/{bookingId}/{commercialId}")]
        public async Task<IActionResult> LinkCommercialToBooking(int bookingId, int commercialId)
        {
            _logger.LogInformation("🔗 [PUT] /api/commercial/link/{BookingId}/{CommercialId} called", bookingId, commercialId);

            try
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("⚠️ Booking {BookingId} not found.", bookingId);
                    return NotFound($"Booking with ID {bookingId} not found.");
                }

                var commercial = await _context.Commercials.FirstOrDefaultAsync(c => c.Id == commercialId);
                if (commercial == null)
                {
                    _logger.LogWarning("⚠️ Commercial {CommercialId} not found.", commercialId);
                    return NotFound($"Commercial with ID {commercialId} not found.");
                }

                booking.CommercialId = commercialId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Linked Booking {BookingId} with Commercial {CommercialId}", bookingId, commercialId);
                return Ok(new
                {
                    message = $"Booking {bookingId} linked with Commercial {commercialId}.",
                    booking.Id,
                    booking.CommercialId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error linking Booking {BookingId} to Commercial {CommercialId}", bookingId, commercialId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
