// Controllers/DashboardController.cs
using HotelAPI.Data;
using HotelAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HotelAPI.Controllers{
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            // Since HotelInfo doesn't have IsActive, we'll count all hotels as active
            // You can modify this logic based on your actual business rules
            var stats = new DashboardStats
            {
                TotalHotels = await _context.HotelInfo.CountAsync(),
                TotalAgencies = await _context.Agencies.CountAsync(),
                ActiveHotels = await _context.HotelInfo.CountAsync(), // All hotels considered active
                PendingApprovals = await _context.Agencies.CountAsync(a => a.IsActive == false),
                TotalCountries = await _context.Countries.CountAsync()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard stats");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("recent-activities")]
    public async Task<IActionResult> GetRecentActivities([FromQuery] int count = 5)
    {
        try
        {
            var hotelActivities = await _context.HotelInfo
                .Include(h => h.Country)
                .OrderByDescending(h => h.CreatedAt)
                .Take(count)
                .Select(h => new RecentActivity
                {
                    Id = h.Id,
                    Type = "hotel",
                    Action = "created",
                    Name = h.HotelName,
                    CountryId = h.Country.Id,
                    Timestamp = h.CreatedAt,
                    TimeAgo = GetTimeAgo(h.CreatedAt)
                })
                .ToListAsync();

            var agencyActivities = await _context.Agencies
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .Select(a => new RecentActivity
                {
                    Id = a.Id,
                    Type = "agency",
                    Action = "created",
                    Name = a.AgencyName,
                    CountryId = a.CountryId.Value,
                    Timestamp = a.CreatedAt,
                    TimeAgo = GetTimeAgo(a.CreatedAt)
                })
                .ToListAsync();

            var allActivities = hotelActivities.Concat(agencyActivities)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToList();

            return Ok(allActivities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent activities");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("hotels-by-country")]
    public async Task<IActionResult> GetHotelsByCountry()
    {
        try
        {
            var hotelsByCountry = await _context.HotelInfo
                .Include(h => h.Country)
                .GroupBy(h => new { h.Country.Id, h.Country.Name })
                .Select(g => new
                {
                    CountryId = g.Key.Id,
                    Country = g.Key.Name,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            return Ok(hotelsByCountry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hotels by country");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("agencies-by-country")]
    public async Task<IActionResult> GetAgenciesByCountry()
    {
        try
        {
            var agenciesByCountry = await _context.Agencies
                .GroupBy(a => a.Country)
                .Select(g => new
                {
                    Country = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            return Ok(agenciesByCountry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching agencies by country");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    // Controllers/DashboardController.cs
  [HttpGet("monthly-stats")]
public async Task<IActionResult> GetMonthlyStats([FromQuery] int months = 6)
{
    try
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-months);
        
        // Generate all months in the range first
        var allMonths = Enumerable.Range(0, months)
            .Select(i => startDate.AddMonths(i))
            .Select(d => new DateTime(d.Year, d.Month, 1))
            .ToList();

        // Get hotel counts by month
        var hotelStats = await _context.HotelInfo
            .Where(h => h.CreatedAt >= startDate && h.CreatedAt <= endDate)
            .GroupBy(h => new { h.CreatedAt.Year, h.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Hotels = g.Count()
            })
            .ToListAsync();

        // Get agency counts by month
        var agencyStats = await _context.Agencies
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Agencies = g.Count()
            })
            .ToListAsync();

        // Combine the results, ensuring all months are included
        var monthlyStats = allMonths.Select(month => new
        {
            Month = month.ToString("MMM yyyy"),
            Hotels = hotelStats
                .FirstOrDefault(h => h.Year == month.Year && h.Month == month.Month)?.Hotels ?? 0,
            Agencies = agencyStats
                .FirstOrDefault(a => a.Year == month.Year && a.Month == month.Month)?.Agencies ?? 0
        })
        .OrderBy(x => x.Month)
        .ToList();

        return Ok(monthlyStats);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching monthly stats");
        return StatusCode(500, $"Internal server error: {ex.Message}");
    }
}
    [HttpGet("top-countries")]
    public async Task<IActionResult> GetTopCountries()
    {
        try
        {
            var topCountries = await _context.Countries

                .Select(c => new
                {
                    CountryId = c.Id,
                    CountryName = c.Name,
                    CountryCode = c.Code,
                    HotelCount = _context.HotelInfo.Count(a => a.CountryId == c.Id),
                    AgencyCount = _context.Agencies.Count(a => a.CountryId == c.Id)
                })
                .OrderByDescending(c => c.HotelCount + c.AgencyCount)
                .Take(8)
                .ToListAsync();

            return Ok(topCountries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top countries");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan <= TimeSpan.FromSeconds(60))
            return $"{timeSpan.Seconds} seconds ago";

        if (timeSpan <= TimeSpan.FromMinutes(60))
            return $"{timeSpan.Minutes} minutes ago";

        if (timeSpan <= TimeSpan.FromHours(24))
            return $"{timeSpan.Hours} hours ago";

        if (timeSpan <= TimeSpan.FromDays(30))
            return $"{timeSpan.Days} days ago";

        if (timeSpan <= TimeSpan.FromDays(365))
            return $"{timeSpan.Days / 30} months ago";

        return $"{timeSpan.Days / 365} years ago";
    }
}
}