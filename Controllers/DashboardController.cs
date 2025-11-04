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
    [Route("api/dashboard")]
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
    // Step 1: Query raw data — only what EF can translate
    var rawData = await _context.Bookings
        .Where(b => b.CheckIn.HasValue)
        .GroupBy(b => new
        {
            Year = b.CheckIn.Value.Year,
            Month = b.CheckIn.Value.Month
        })
        .Select(g => new
        {
            g.Key.Year,
            g.Key.Month,
            TotalBookings = g.Count()
        })
        .OrderBy(g => g.Year)
        .ThenBy(g => g.Month)
        .ToListAsync();  // ✅ query executed here in SQL

    // Step 2: Format the month string on the client (in memory)
    var formattedData = rawData
        .Select(g => new
        {
            Month = new DateTime(g.Year, g.Month, 1).ToString("MMM yyyy"),
            g.TotalBookings
        })
        .ToList();

    return Ok(formattedData);
}


        // ===============================================================
        // 4️⃣ FINANCIAL TRENDS (LINE CHART)
        // ===============================================================
[HttpGet("financial-trends")]
public async Task<IActionResult> GetFinancialTrends()
{
    try
    {
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        // 1️⃣ Run the database query fully async (server-side) // new
        var grouped = await _context.Commercials
            .Where(c => c.CreatedAt >= sixMonthsAgo)
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(e => (decimal?)e.SellingPrice ?? 0),
                Cost = g.Sum(e => (decimal?)e.BuyingAmount ?? 0),
                Profit = g.Sum(e => (decimal?)e.Profit ?? 0),
                AvgProfitMargin = g.Average(e => (decimal?)e.ProfitMarginPercent ?? 0),
                AvgMarkup = g.Average(e => (decimal?)e.MarkupPercent ?? 0),
                AvgVatPercent = g.Average(e => (decimal?)e.SellingVatPercent ?? 0)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        // 2️⃣ Format the results in-memory (client-side)
        var trends = grouped
            .Select(x => new
            {
                Month = new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy"),
                x.Revenue,
                x.Cost,
                x.Profit,
                x.AvgProfitMargin,
                x.AvgMarkup,
                x.AvgVatPercent
            })
            .ToList();

        return Ok(trends);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = "Error fetching financial trends",
            error = ex.Message
        });
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
        [HttpGet("upcoming-deadlines")]
        public async Task<IActionResult> GetUpcomingDeadlines()
        {
            try
            {
                var now = DateTime.UtcNow;
                var threeDaysLater = now.AddDays(3);

                var deadlines = await _context.Bookings
                    .Where(b => b.Deadline != null && b.Deadline >= now && b.Deadline <= threeDaysLater)
                    .Select(b => new
                    {
                        b.Id,
                        b.TicketNumber,
                        HotelName = b.Hotel != null ? b.Hotel.HotelName : "Unknown Hotel",
                        AgencyName = b.Agency != null ? b.Agency.AgencyName : "Unknown Agency",
                        b.Status,
                        b.Deadline
                    })
                    .OrderBy(b => b.Deadline)
                    .ToListAsync();

                return Ok(new
                {
                    Success = true,
                    Count = deadlines.Count,
                    Data = deadlines
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error fetching upcoming deadlines",
                    Error = ex.Message
                });
            }
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
    // ===============================================================
// 9️⃣ RECENT ACTIVITIES (COMPACT FEED)
// ===============================================================
[HttpGet("recent-activities")]
public async Task<IActionResult> GetRecentActivities()
{
    try
    {
        var recent = await _context.RecentActivities
            .OrderByDescending(r => r.Timestamp)
            .Take(8) // Compact: limit to 8 latest actions
            .Select(r => new
            {
                r.Id,
                r.UserName,
                r.ActionType,
                r.TableName,
                r.RecordId,
                // Trim long descriptions for compact UI
                Description = r.Description.Length > 70 
                    ? r.Description.Substring(0, 67) + "..." 
                    : r.Description,
                TimeAgo = GetTimeAgo(r.Timestamp)
            })
            .ToListAsync();

        return Ok(new
        {
            Success = true,
            Count = recent.Count,
            Data = recent
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            Success = false,
            Message = "Error loading recent activities",
            Error = ex.Message
        });
    }
}

        
    }
}
