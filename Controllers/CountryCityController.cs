using System.Security.Claims;
using HotelAPI.Data;
using HotelAPI.Models;
using HotelAPI.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class CountryCityController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CountryCityController(AppDbContext context)
        {
            _context = context;
        }

        private async Task LogRecentActivityAsync(string entity, int entityId, string action, string description)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            string userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var activity = new RecentActivity
            {
                UserId = userId,
                UserName = userName,
                Entity = entity,
                EntityId = entityId,
                Action = action,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            _context.RecentActivities.Add(activity);
            await _context.SaveChangesAsync();
            
        }

        // GET: api/countries
        [HttpGet("countries")]
        public async Task<ActionResult<IEnumerable<CountryDto>>> GetCountries()
        {
            var countries = await _context.Countries
                .Include(c => c.Cities)
                
                .ToListAsync();

            var countryDtos = countries.Select(c => new CountryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Flag = c.Flag,
                PhoneCode = c.PhoneCode,
                PhoneNumberDigits = c.PhoneNumberDigits,
                Cities = c.Cities.Select(city => new CityDto
                {
                    Id = city.Id,
                    Name = city.Name
                }).ToList()
            }).ToList();

            return Ok(countryDtos);
        }

        // GET: api/countries/{id}
        [HttpGet("countries/{id}")]
        public async Task<ActionResult<CountryDto>> GetCountry(int id)
        {
            var country = await _context.Countries
                .Include(c => c.Cities)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (country == null)
                return NotFound();

            var dto = new CountryDto
            {
                Id = country.Id,
                Name = country.Name,
                Code = country.Code,
                Cities = country.Cities.Select(city => new CityDto
                {
                    Id = city.Id,
                    Name = city.Name,
                    CountryId = city.CountryId
                }).ToList()
            };

            return Ok(dto);
}

        // POST: api/countries
        [HttpPost("countries")]
        public async Task<ActionResult<CountryDto>> AddCountry([FromBody] CountryDto dto)
        {
            var exists = await _context.Countries.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower());
            if (exists) return BadRequest(new { message = "Country already exists" });

            var country = new Country
            {
                Name = dto.Name,
                Code = dto.Code
            };

            _context.Countries.Add(country);
            await _context.SaveChangesAsync();
            await LogRecentActivityAsync("Country", country.Id, "CREATE", $"{country.Name} created");
            dto.Id = country.Id;
            return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, dto);
        }

        // PUT: api/countries/{id}
        [HttpPut("countries/{id}")]
        public async Task<IActionResult> UpdateCountry(int id, [FromBody] CountryDto dto)
        {
            var country = await _context.Countries.FindAsync(id);
            if (country == null)
                return NotFound(new { message = "Country not found" });

            // Prevent duplicate names
            var exists = await _context.Countries
                .AnyAsync(c => c.Id != id && c.Name.ToLower() == dto.Name.ToLower());
            if (exists)
                return BadRequest(new { message = "Another country with same name exists" });

            country.Name = dto.Name;
            country.Code = dto.Code;

            await _context.SaveChangesAsync();
            await LogRecentActivityAsync("Country", country.Id, "UPDATE", $"{country.Name} updated");

            return Ok(new { message = "Country updated successfully" });
        }

        // DELETE: api/countries/{id}
        [HttpDelete("countries/{id}")]
        public async Task<IActionResult> DeleteCountry(int id)
        {
            var country = await _context.Countries
                .Include(c => c.Cities)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (country == null)
                return NotFound(new { message = "Country not found" });

            if (country.Cities.Any())
                return BadRequest(new { message = "Cannot delete country with cities. Delete cities first." });

            _context.Countries.Remove(country);
            await _context.SaveChangesAsync();
            await LogRecentActivityAsync("Country", country.Id, "DELETE", $"{country.Name} deleted");

            return Ok(new { message = "Country deleted successfully" });
        }

        // GET: api/cities/by-country/{countryId}
        [HttpGet("cities/by-country/{countryId}")]
        public async Task<ActionResult<IEnumerable<CityDto>>> GetCitiesByCountry(int countryId)
        {
            var cities = await _context.Cities
                .Where(c => c.CountryId == countryId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var cityDtos = cities.Select(c => new CityDto
            {
                Id = c.Id,
                Name = c.Name,
                CountryId = c.CountryId
            }).ToList();

            return Ok(cityDtos);
        }

        // POST: api/cities
        [HttpPost("cities")]
        public async Task<ActionResult<CityDto>> AddCity([FromBody] CityDto dto)
        {
            var countryExists = await _context.Countries.AnyAsync(c => c.Id == dto.CountryId);
            if (!countryExists)
                return BadRequest(new { message = "Country does not exist" });

            var exists = await _context.Cities.AnyAsync(c =>
                c.Name.ToLower() == dto.Name.ToLower() && c.CountryId == dto.CountryId);
            if (exists)
                return BadRequest(new { message = "City already exists for this country" });

            var city = new City
            {
                Name = dto.Name,
                CountryId = dto.CountryId
            };

            _context.Cities.Add(city);
            await _context.SaveChangesAsync();
            await LogRecentActivityAsync("City", city.Id, "CREATE", $"{city.Name} created in country ID {city.CountryId}");

            dto.Id = city.Id;
            return CreatedAtAction(nameof(GetCitiesByCountry), new { countryId = dto.CountryId }, dto);
        }

        // PUT: api/cities/{id}
        [HttpPut("cities/{id}")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] CityDto dto)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null)
                return NotFound(new { message = "City not found" });

            // Prevent duplicate city names in the same country
            var exists = await _context.Cities
                .AnyAsync(c => c.Id != id && c.Name.ToLower() == dto.Name.ToLower() && c.CountryId == dto.CountryId);
            if (exists)
                return BadRequest(new { message = "Another city with same name exists in this country" });

            city.Name = dto.Name;
            city.CountryId = dto.CountryId;

            await _context.SaveChangesAsync();
            await LogRecentActivityAsync("City", city.Id, "UPDATE", $"{city.Name} updated in country ID {city.CountryId}");
            return Ok(new { message = "City updated successfully" });
        }

        // DELETE: api/cities/{id}
        [HttpDelete("cities/{id}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null)
                return NotFound(new { message = "City not found" });

            // Optional: check if any hotels exist in this city before deletion
            var hasHotels = await _context.HotelInfo.AnyAsync(h => h.CityId == id);
            if (hasHotels)
                return BadRequest(new { message = "Cannot delete city with hotels. Delete hotels first." });

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();
            await LogRecentActivityAsync("City", city.Id, "DELETE", $"{city.Name} deleted from country ID {city.CountryId}");
            return Ok(new { message = "City deleted successfully" });
        }
        // GET: api/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            var totalHotels = await _context.HotelInfo.CountAsync();
            var activeContacts = await _context.HotelStaff.CountAsync();
            var totalCountries = await _context.Countries.CountAsync();
            var newThisMonth = await _context.HotelInfo
                                            .Where(h => h.CreatedAt.Month == DateTime.UtcNow.Month &&
                                                        h.CreatedAt.Year == DateTime.UtcNow.Year)
                                            .CountAsync();

            return Ok(new
            {
                totalHotels,
                activeContacts,
                totalCountries,
                newThisMonth
            });
        }



    }
}
