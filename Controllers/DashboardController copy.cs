// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using HotelAPI.Data;
// using System;
// using System.Linq;

// namespace HotelAPI.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class DashboardController : ControllerBase
//     {
//         private readonly AppDbContext _context;
//         public DashboardController(AppDbContext context) => _context = context;

//         // ============================================================
//         // 1️⃣  SUMMARY METRICS
//         // ============================================================
//         [HttpGet("summary")]
//         public IActionResult GetSummary()
//         {
//             var totalBookings = _context.Bookings.Count();
//             var confirmed = _context.Bookings.Count(b => b.Status == "Confirmed");
//             var cancelled = _context.Bookings.Count(b => b.Status == "Cancelled");
//             var pending = _context.Bookings.Count(b => b.Status == "Pending");
//             var activeAgencies = _context.Agencies.Count(a => a.IsActive);
//             var activeSuppliers = _context.Suppliers.Count(s => s.IsActive);
//             var activeHotels = _context.HotelInfo.Count(h => h.IsActive);

//             var today = DateTime.UtcNow.Date;
//             var todayBookings = _context.Bookings.Count(b => b.CreatedAt.Date == today);

//             return Ok(new
//             {
//                 totalBookings,
//                 confirmed,
//                 pending,
//                 cancelled,
//                 activeAgencies,
//                 activeSuppliers,
//                 activeHotels,
//                 todayBookings
//             });
//         }

//         // ============================================================
//         // 2️⃣  MONTHLY BOOKING TREND
//         // ============================================================
//         [HttpGet("booking-trends")]
//         public IActionResult GetBookingTrends()
//         {
//             var data = _context.Bookings
//                 .Where(b => b.CheckIn.HasValue)
//                 .GroupBy(b => new { b.CheckIn.Value.Year, b.CheckIn.Value.Month })
//                 .Select(g => new
//                 {
//                     Year = g.Key.Year,
//                     Month = g.Key.Month,
//                     TotalBookings = g.Count(),
//                     Confirmed = g.Count(b => b.Status == "Confirmed"),
//                     Cancelled = g.Count(b => b.Status == "Cancelled")
//                 })
//                 .OrderBy(x => x.Year).ThenBy(x => x.Month)
//                 .ToList();

//             return Ok(data);
//         }

//         // ============================================================
//         // 3️⃣  AGENCY PERFORMANCE (for bar chart)
//         // ============================================================
//         [HttpGet("agency-performance")]
//         public IActionResult GetAgencyPerformance()
//         {
//             var data = _context.Bookings
//                 .Include(b => b.Agency)
//                 .Where(b => b.Agency != null)
//                 .GroupBy(b => b.Agency.AgencyName)
//                 .Select(g => new
//                 {
//                     Agency = g.Key,
//                     Bookings = g.Count(),
//                     Confirmed = g.Count(b => b.Status == "Confirmed"),
//                     Cancelled = g.Count(b => b.Status == "Cancelled")
//                 })
//                 .OrderByDescending(x => x.Bookings)
//                 .Take(10)
//                 .ToList();

//             return Ok(data);
//         }

//         // ============================================================
//         // 4️⃣  SUPPLIER PERFORMANCE
//         // ============================================================
//         [HttpGet("supplier-performance")]
//         public IActionResult GetSupplierPerformance()
//         {
//             var data = _context.Bookings
//                 .Include(b => b.Supplier)
//                 .Where(b => b.Supplier != null)
//                 .GroupBy(b => b.Supplier.SupplierName)
//                 .Select(g => new
//                 {
//                     Supplier = g.Key,
//                     Bookings = g.Count(),
//                     Confirmed = g.Count(b => b.Status == "Confirmed"),
//                     Cancelled = g.Count(b => b.Status == "Cancelled")
//                 })
//                 .OrderByDescending(x => x.Bookings)
//                 .Take(10)
//                 .ToList();

//             return Ok(data);
//         }

//         // ============================================================
//         // 5️⃣  HOTEL OCCUPANCY HEATMAP DATA
//         // ============================================================
//         [HttpGet("hotel-occupancy")]
//         public IActionResult GetHotelOccupancy()
//         {
//             var data = _context.Bookings
//                 .Include(b => b.Hotel)
//                 .Where(b => b.Hotel != null && b.CheckIn.HasValue)
//                 .GroupBy(b => b.Hotel.HotelName)
//                 .Select(g => new
//                 {
//                     Hotel = g.Key,
//                     Bookings = g.Count(),
//                     AvgNights = g.Average(x => x.Nights ?? 0),
//                     People = g.Sum(x => x.NumberOfPeople ?? 0)
//                 })
//                 .OrderByDescending(x => x.Bookings)
//                 .Take(10)
//                 .ToList();

//             return Ok(data);
//         }

//         // ============================================================
//         // 6️⃣  TOP 5 PERFORMERS (Hotels, Agencies, Suppliers)
//         // ============================================================
//         [HttpGet("top-performers")]
//         public IActionResult GetTopPerformers()
//         {
//             var topHotels = _context.Bookings
//                 .Include(b => b.Hotel)
//                 .GroupBy(b => b.Hotel.HotelName)
//                 .Select(g => new { Hotel = g.Key, Count = g.Count() })
//                 .OrderByDescending(x => x.Count).Take(5).ToList();

//             var topAgencies = _context.Bookings
//                 .Include(b => b.Agency)
//                 .GroupBy(b => b.Agency.AgencyName)
//                 .Select(g => new { Agency = g.Key, Count = g.Count() })
//                 .OrderByDescending(x => x.Count).Take(5).ToList();

//             var topSuppliers = _context.Bookings
//                 .Include(b => b.Supplier)
//                 .GroupBy(b => b.Supplier.SupplierName)
//                 .Select(g => new { Supplier = g.Key, Count = g.Count() })
//                 .OrderByDescending(x => x.Count).Take(5).ToList();

//             return Ok(new { topHotels, topAgencies, topSuppliers });
//         }

//         // ============================================================
//         // 7️⃣  DAILY BOOKINGS (for line chart)
//         // ============================================================
//         [HttpGet("daily-bookings")]
//         public IActionResult GetDailyBookings()
//         {
//             var today = DateTime.UtcNow;
//             var last30 = today.AddDays(-30);

//             var data = _context.Bookings
//                 .Where(b => b.CreatedAt >= last30)
//                 .GroupBy(b => b.CreatedAt.Date)
//                 .Select(g => new
//                 {
//                     Date = g.Key,
//                     Count = g.Count(),
//                     Confirmed = g.Count(x => x.Status == "Confirmed")
//                 })
//                 .OrderBy(x => x.Date)
//                 .ToList();

//             return Ok(data);
//         }

//         // ============================================================
//         // 8️⃣  BOOKINGS BY COUNTRY (for map/chart)
//         // ============================================================
//         [HttpGet("bookings-by-country")]
//         public IActionResult GetBookingsByCountry()
//         {
//             var data = _context.Bookings
//                 .Include(b => b.Hotel)
//                 .ThenInclude(h => h.Country)
//                 .Where(b => b.Hotel != null && b.Hotel.Country != null)
//                 .GroupBy(b => b.Hotel.Country.Name)
//                 .Select(g => new
//                 {
//                     Country = g.Key,
//                     Count = g.Count()
//                 })
//                 .OrderByDescending(x => x.Count)
//                 .ToList();

//             return Ok(data);
//         }

//         // ============================================================
//         // 9️⃣  UPCOMING BOOKINGS
//         // ============================================================
//         [HttpGet("upcoming-bookings")]
//         public IActionResult GetUpcoming()
//         {
//             var today = DateTime.UtcNow;
//             var upcoming = _context.Bookings
//                 .Include(b => b.Agency)
//                 .Include(b => b.Hotel)
//                 .Where(b => b.CheckIn >= today)
//                 .OrderBy(b => b.CheckIn)
//                 .Take(20)
//                 .Select(b => new
//                 {
//                     b.TicketNumber,
//                     b.Status,
//                     Agency = b.Agency.AgencyName,
//                     Hotel = b.Hotel.HotelName,
//                     b.CheckIn,
//                     b.CheckOut
//                 }).ToList();

//             return Ok(upcoming);
//         }

//         // ============================================================
//         // 🔟  BOOKING STATUS DISTRIBUTION (Pie chart)
//         // ============================================================
//         [HttpGet("status-distribution")]
//         public IActionResult GetStatusDistribution()
//         {
//             var data = _context.Bookings
//                 .GroupBy(b => b.Status)
//                 .Select(g => new
//                 {
//                     Status = g.Key,
//                     Count = g.Count()
//                 })
//                 .ToList();

//             return Ok(data);
//         }

//         // ============================================================
//         // 11️⃣  PEAK SEASON ANALYSIS
//         // ============================================================
//         [HttpGet("peak-season")]
//         public IActionResult GetPeakSeason()
//         {
//             var peak = _context.Bookings
//                 .Where(b => b.CheckIn.HasValue)
//                 .GroupBy(b => b.CheckIn.Value.Month)
//                 .Select(g => new { Month = g.Key, Count = g.Count() })
//                 .OrderByDescending(x => x.Count)
//                 .FirstOrDefault();

//             string monthName = "N/A";

//             if (peak != null)
//             {
//                 // peak.Month is already int, no need for .Value
//                 monthName = new DateOnly(2025, peak.Month, 1).ToString("MMMM");
//             }

//             return Ok(new
//             {
//                 PeakMonth = peak?.Month,
//                 PeakMonthName = monthName,
//                 TotalBookings = peak?.Count
//             });
//         }




//         // ============================================================
//         // 12️⃣  CUSTOMER VOLUME BY PEOPLE COUNT
//         // ============================================================
//         [HttpGet("people-distribution")]
//         public IActionResult GetPeopleDistribution()
//         {
//             var data = _context.Bookings
//                 .GroupBy(b => b.NumberOfPeople)
//                 .Select(g => new { People = g.Key, Count = g.Count() })
//                 .OrderBy(x => x.People)
//                 .ToList();

//             return Ok(data);
//         }

//         // ============================================================
//         // 13️⃣  AVERAGE STAY & PEOPLE METRICS
//         // ============================================================
//         [HttpGet("averages")]
//         public IActionResult GetAverages()
//         {
//             var avgPeople = _context.Bookings.Average(b => b.NumberOfPeople ?? 0);
//             var avgNights = _context.Bookings.Average(b => b.Nights ?? 0);

//             return Ok(new
//             {
//                 AveragePeople = Math.Round(avgPeople, 2),
//                 AverageNights = Math.Round(avgNights, 2)
//             });
//         }


//         // ============================================================
//         // 15️⃣  RECENT ACTIVITY FEED
//         // ============================================================
//         [HttpGet("recent-activity")]
//         public IActionResult GetRecentActivity()
//         {
//             var activities = _context.Bookings
//                 .OrderByDescending(b => b.UpdatedAt)
//                 .Take(10)
//                 .Select(b => new
//                 {
//                     Message = $"Booking #{b.TicketNumber} was {b.Status}",
//                     b.UpdatedAt
//                 }).ToList();

//             return Ok(activities);
//         }
//     }
// }
