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
// ============================================================
// 🧮 Helper: Full financial computation inside Controller
// ============================================================
        private void ComputeCommercialFields(Commercial c)
        {
            if (c == null) return;

            // --- Buying side ---
            decimal vatBuy = c.BuyingVatIncluded ? (c.BuyingAmount * c.BuyingVatPercent / 100) : 0;
            c.NetBuying = c.BuyingVatIncluded
                ? c.BuyingAmount / (1 + (c.BuyingVatPercent / 100))
                : c.BuyingAmount;
            c.GrossBuying = c.NetBuying + vatBuy;

            // --- Selling side ---
            decimal vatSell = c.SellingVatIncluded ? (c.SellingPrice * c.SellingVatPercent / 100) : 0;
            c.NetSelling = c.SellingVatIncluded
                ? c.SellingPrice / (1 + (c.SellingVatPercent / 100))
                : c.SellingPrice;
            c.GrossSelling = c.NetSelling + vatSell;

            // --- Apply Commission (buying side) ---
            decimal commissionAmt = 0;
            if (c.Commissionable && c.CommissionValue.HasValue)
            {
                commissionAmt = c.CommissionType?.ToLower() == "percentage"
                    ? c.NetBuying * (c.CommissionValue.Value / 100)
                    : c.CommissionValue.Value;
            }
            c.NetBuying += commissionAmt; // commission adds to cost

            // --- Apply Incentive (selling side) ---
            decimal incentiveAmt = 0;
            if (c.Incentive && c.IncentiveValue.HasValue)
            {
                incentiveAmt = c.IncentiveType?.ToLower() == "percentage"
                    ? c.NetSelling * (c.IncentiveValue.Value / 100)
                    : c.IncentiveValue.Value;
            }
            c.NetSelling += incentiveAmt; // incentive increases revenue

            // --- Currency Conversion ---
            decimal effectiveRate = c.ExchangeRate.HasValue && c.ExchangeRate > 0
                ? c.ExchangeRate.Value
                : 1;

            decimal convertedBuying = c.NetBuying * effectiveRate;
            decimal convertedSelling = c.NetSelling * effectiveRate;

            // --- Profit & Ratios ---
            c.Profit = convertedSelling - convertedBuying;
            c.ProfitMarginPercent = convertedSelling != 0 ? (c.Profit / convertedSelling) * 100 : 0;
            c.MarkupPercent = convertedBuying != 0 ? (c.Profit / convertedBuying) * 100 : 0;

            c.UpdatedAt = DateTime.UtcNow;
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
                    return BadRequest(new { error = "Invalid payload. BookingId is required." });
                }

                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == dto.BookingId);
                if (booking == null)
                {
                    string message = $"Booking with ID {dto.BookingId} not found (404).";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
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
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
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
                    return BadRequest(new { error = "Invalid pagination parameters." });

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

                if (data == null || data.Count == 0)
                {
                    string message = "No commercial records found (404).";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
                }

                var total = await _context.Commercials.CountAsync();
                _logger.LogInformation("✅ Returned {Count} records (Total={Total})", data.Count, total);

                return Ok(new { total, page, pageSize, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error fetching all commercials.");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
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
                    string message = $"Commercial with ID {id} not found (404).";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
                }

                _logger.LogInformation("✅ Commercial ID {Id} found.", id);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error fetching Commercial ID {Id}", id);
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
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
                    return BadRequest(new { error = "Invalid BookingId." });
                }

                var bookingExists = await _context.Bookings.AnyAsync(b => b.Id == bookingId);
                if (!bookingExists)
                {
                    string message = $"Booking ID {bookingId} not found (404).";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
                }

                var data = await _context.Commercials
                    .Include(c => c.Booking)
                    .FirstOrDefaultAsync(c => c.BookingId == bookingId);

                if (data == null)
                {
                    string message = $"No commercial found for Booking ID {bookingId} (404).";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
                }

                _logger.LogInformation("✅ Found Commercial ID {CommercialId} for BookingId {BookingId}.", data.Id, bookingId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception while fetching commercial by bookingId {BookingId}", bookingId);
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
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
                    string message = $"Commercial with ID {id} not found (404) - cannot update.";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
                }

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
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
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
                    return BadRequest(new { error = "Search query is required." });

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

                if (results == null || results.Count == 0)
                {
                    string message = $"No commercial records match search '{query}' (404).";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
                }

                _logger.LogInformation("✅ Search returned {Count} results for query '{Query}'", results.Count, query);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error searching Commercials with query '{Query}'", query);
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        // -------------------- SUMMARY --------------------
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            _logger.LogInformation("📊 [GET] /api/commercial/summary called");

            try
            {
                var total = await _context.Commercials.CountAsync();
                if (total == 0)
                {
                    string message = "No commercial data available to summarize (404).";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
                }

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
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
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
                    string message = $"Booking with ID {bookingId} not found (404) - cannot link.";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
                }

                var commercial = await _context.Commercials.FirstOrDefaultAsync(c => c.Id == commercialId);
                if (commercial == null)
                {
                    string message = $"Commercial with ID {commercialId} not found (404) - cannot link.";
                    _logger.LogWarning(message);
                    return NotFound(new { error = message });
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
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }
        [HttpGet("bookings-dropdown")]
public async Task<IActionResult> GetBookingsDropdown()
{
    var bookings = await _context.Bookings
        .Include(b => b.Hotel)
        .Include(b => b.Agency)
        .OrderByDescending(b => b.Id)
        .Select(b => new
        {
            id = b.Id,
            ticketNumber = b.TicketNumber,
            hotel = b.Hotel != null ? b.Hotel.HotelName : "N/A",
            agency = b.Agency != null ? b.Agency.AgencyName : "N/A",
            supplier = b.Supplier != null ? b.Supplier.SupplierName : "N/A",
            checkIn = b.CheckIn,
            checkOut = b.CheckOut

        })
        .ToListAsync();

    return Ok(bookings);
}

    }
}
