using HotelAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // ===============================================================
        // 1️⃣ OVERALL SUMMARY (KPI CARDS)
        // ===============================================================
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var totalBookings = await _context.Bookings.CountAsync();
                var totalHotels = await _context.HotelInfo.CountAsync(h => h.IsActive);
                var totalSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive);
                var totalAgencies = await _context.Agencies.CountAsync(a => a.IsActive);
                var totalRevenue = await _context.Commercials.SumAsync(c => (decimal?)c.SellingPrice) ?? 0;
                var totalCost = await _context.Commercials.SumAsync(c => (decimal?)c.BuyingAmount) ?? 0;
                var totalProfit = totalRevenue - totalCost;

                return Ok(new
                {
                    Cards = new
                    {
                        TotalBookings = totalBookings,
                        TotalHotels = totalHotels,
                        TotalSuppliers = totalSuppliers,
                        TotalAgencies = totalAgencies,
                        TotalRevenue = totalRevenue,
                        TotalCost = totalCost,
                        TotalProfit = totalProfit
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading dashboard summary", error = ex.Message });
            }
        }

        // ===============================================================
        // 2️⃣ BOOKING STATUS DISTRIBUTION (PIE / DONUT)
        // ===============================================================
        [HttpGet("booking-status")]
        public async Task<IActionResult> GetBookingStatus()
        {
            var data = await _context.Bookings
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return Ok(data);
        }

        // ===============================================================
        // 3️⃣ MONTHLY BOOKINGS TREND (LINE CHART)
        // ===============================================================
 [HttpGet("bookings-trend")]
public async Task<IActionResult> GetBookingsTrend()
{
    var data = await _context.Bookings
        .Where(b => b.CheckIn.HasValue) // ✅ only take rows with CheckIn date
        .GroupBy(b => new { 
            Year = b.CheckIn.Value.Year, 
            Month = b.CheckIn.Value.Month 
        })
        .Select(g => new
        {
            Month = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
            TotalBookings = g.Count()
        })
        .OrderBy(g => g.Month)
        .ToListAsync();

    return Ok(data);
}

        // ===============================================================
        // 4️⃣ FINANCIAL TRENDS (LINE CHART)
        // ===============================================================
        [HttpGet("financial-trend")]
        public async Task<IActionResult> GetFinancialTrend()
        {
            try
            {
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

                var data = await _context.Commercials
                    .Where(c => c.CreatedAt >= sixMonthsAgo)
                    .GroupBy(c => new
                    {
                        Year = c.CreatedAt.Year,
                        Month = c.CreatedAt.Month
                    })
                    .Select(g => new
                    {
                        Month = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
                        Revenue = g.Sum(x => (decimal?)x.SellingPrice ?? 0),
                        Cost = g.Sum(x => (decimal?)x.BuyingAmount ?? 0),
                        Profit = g.Sum(x => (decimal?)x.Profit ?? 0),
                        AvgProfitMargin = g.Average(x => (decimal?)x.ProfitMarginPercent ?? 0),
                        AvgMarkup = g.Average(x => (decimal?)x.MarkupPercent ?? 0),
                        AvgVatPercent = g.Average(x => (decimal?)x.SellingVatPercent ?? 0)
                    })
                    .OrderBy(g => g.Month)
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching financial trends", error = ex.Message });
            }
        }

        // ===============================================================
        // 5️⃣ REVENUE BY COUNTRY (MAP / BAR CHART)
        // ===============================================================
        [HttpGet("revenue-by-country")]
        public async Task<IActionResult> GetRevenueByCountry()
        {
            var data = await _context.Bookings
                .Include(b => b.Hotel).ThenInclude(h => h.Country)
                .Include(b => b.Commercial)
                .Where(b => b.Hotel != null && b.Hotel.Country != null)
                .GroupBy(b => b.Hotel.Country.Name)
                .Select(g => new
                {
                    Country = g.Key,
                    TotalRevenue = g.Sum(x => (decimal?)(x.Commercial != null ? x.Commercial.SellingPrice : 0)) ?? 0,
                    BookingCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToListAsync();

            return Ok(data);
        }

        // ===============================================================
        // 6️⃣ TOP AGENCIES (BAR CHART)
        // ===============================================================
        [HttpGet("top-agencies")]
        public async Task<IActionResult> GetTopAgencies()
        {
            var data = await _context.Bookings
                .Include(b => b.Agency)
                .Where(b => b.Agency != null)
                .GroupBy(b => b.Agency.AgencyName)
                .Select(g => new { Agency = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            return Ok(data);
        }

        // ===============================================================
        // 7️⃣ TOP SUPPLIERS (BAR / PIE CHART)
        // ===============================================================
        [HttpGet("top-suppliers")]
        public async Task<IActionResult> GetTopSuppliers()
        {
            var data = await _context.Bookings
                .Include(b => b.Supplier)
                .Where(b => b.Supplier != null)
                .GroupBy(b => b.Supplier.SupplierName)
                .Select(g => new { Supplier = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            return Ok(data);
        }

        // ===============================================================
        // 8️⃣ RECENT BOOKINGS (LIST)
        // ===============================================================
        [HttpGet("recent-bookings")]
        public async Task<IActionResult> GetRecentBookings()
        {
            var data = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Agency)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new
                {
                    b.Id,
                    b.TicketNumber,
                    Hotel = b.Hotel.HotelName,
                    Agency = b.Agency.AgencyName,
                    b.CheckIn,
                    b.CheckOut,
                    b.Status
                })
                .ToListAsync();

            return Ok(data);
        }

        // ===============================================================
        // 9️⃣ ACTIVITY FEED (TIMELINE)
        // ===============================================================
        [HttpGet("recent-activities")]
        public async Task<IActionResult> GetRecentActivities()
        {
            var recent = await _context.RecentActivities
                .OrderByDescending(a => a.Timestamp)
                .Take(15)
                .Select(a => new
                {
                    a.UserName,
                    a.Action,
                    a.Entity,
                    a.Description,
                    a.Timestamp,
                    TimeAgo = GetTimeAgo(a.Timestamp)
                })
                .ToListAsync();

            return Ok(recent);
        }

        // ===============================================================
        // 🔟 WEEKLY BOOKINGS (7-DAY TREND)
        // ===============================================================
        [HttpGet("weekly-bookings")]
        public async Task<IActionResult> GetWeeklyBookings()
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var data = await _context.Bookings
                .Where(b => b.CreatedAt >= sevenDaysAgo)
                .GroupBy(b => b.CreatedAt.Date)
                .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return Ok(data);
        }

        // ===============================================================
        // 1️⃣1️⃣ AGENCY PERFORMANCE (REVENUE / PROFIT BY AGENCY)
        // ===============================================================
        [HttpGet("agency-performance")]
        public async Task<IActionResult> GetAgencyPerformance()
        {
            var data = await _context.Bookings
                .Include(b => b.Agency)
                .Include(b => b.Commercial)
                .Where(b => b.Agency != null && b.Commercial != null)
                .GroupBy(b => b.Agency.AgencyName)
                .Select(g => new
                {
                    Agency = g.Key,
                    Revenue = g.Sum(x => (decimal?)x.Commercial.SellingPrice ?? 0),
                    Profit = g.Sum(x => (decimal?)x.Commercial.Profit ?? 0),
                    Bookings = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            return Ok(data);
        }

        // ===============================================================
        // Helper
        // ===============================================================
        private static string GetTimeAgo(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;
            if (diff.TotalSeconds < 60) return $"{diff.Seconds}s ago";
            if (diff.TotalMinutes < 60) return $"{diff.Minutes}m ago";
            if (diff.TotalHours < 24) return $"{diff.Hours}h ago";
            if (diff.TotalDays < 30) return $"{diff.Days}d ago";
            if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)}mo ago";
            return $"{(int)(diff.TotalDays / 365)}y ago";
        }
    }
}
