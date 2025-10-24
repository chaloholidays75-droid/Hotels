using HotelAPI.Data;
using HotelAPI.Models;
using HotelAPI.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Route("api/agencyStaff")]
    [Authorize] // Keep this if JWT is enabled
    public class AgencyStaffController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AgencyStaffController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ FIXED: Nullable return type
        private int? GetUserId()
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userId) ? null : int.Parse(userId);
        }

        // ✅ Get all staff for an agency WITH agencyName
        [HttpGet("agency/{agencyId}")]
        public async Task<IActionResult> GetByAgency(int agencyId)
        {
            var staff = await _context.AgencyStaff
                .Where(s => s.AgencyId == agencyId)
                .Include(s => s.Agency)
                .OrderBy(s => s.Role)
                .Select(s => new AgencyStaffReadDto
                {
                    Id = s.Id,
                    AgencyId = s.AgencyId,
                    AgencyName = s.Agency != null ? s.Agency.AgencyName : null, // ✅ Null safe
                    Role = s.Role,
                    Designation = s.Designation,
                    Name = s.Name,
                    Email = s.Email,
                    Phone = s.Phone,
                    CreatedById = s.CreatedById,
                    UpdatedById = s.UpdatedById,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return Ok(staff);
        }

        // ✅ Get single staff by ID WITH agencyName
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var staff = await _context.AgencyStaff
                .Include(s => s.Agency)
                .Where(s => s.Id == id)
                .Select(s => new AgencyStaffReadDto
                {
                    Id = s.Id,
                    AgencyId = s.AgencyId,
                    AgencyName = s.Agency != null ? s.Agency.AgencyName : null, // ✅ Null safe
                    Role = s.Role,
                    Designation = s.Designation,
                    Name = s.Name,
                    Email = s.Email,
                    Phone = s.Phone,
                    CreatedById = s.CreatedById,
                    UpdatedById = s.UpdatedById,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (staff == null) return NotFound();
            return Ok(staff);
        }

        // ✅ Create new staff
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AgencyStaffCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            bool exists = await _context.AgencyStaff
                .AnyAsync(s => s.AgencyId == dto.AgencyId &&
                            s.Name.ToLower() == dto.Name.ToLower() &&
                            s.Role.ToLower() == dto.Role.ToLower());

            if (exists)
                return BadRequest("Staff already exists for this agency with same name and role.");


            var staff = new AgencyStaff
            {
                AgencyId = dto.AgencyId,
                Role = dto.Role,
                Designation = dto.Designation,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                CreatedById = GetUserId(), // ✅ Safe nullable assignment
                CreatedAt = DateTime.UtcNow
            };

            _context.AgencyStaff.Add(staff);
            await _context.SaveChangesAsync();
            return Ok(staff);
        }

        // ✅ Update existing staff
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AgencyStaffUpdateDto dto)
        {
            var staff = await _context.AgencyStaff.FindAsync(id);
            if (staff == null) return NotFound();
            bool exists = await _context.AgencyStaff
                .AnyAsync(s => s.Id != id &&
                            s.AgencyId == staff.AgencyId &&
                            s.Name.ToLower() == dto.Name.ToLower() &&
                            s.Role.ToLower() == dto.Role.ToLower());

            if (exists)
                return BadRequest("Staff already exists for this agency with same name and role.");


            staff.Role = dto.Role;
            staff.Designation = dto.Designation;
            staff.Name = dto.Name;
            staff.Email = dto.Email;
            staff.Phone = dto.Phone;
            staff.UpdatedById = GetUserId();
            staff.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(staff);
        }

        // ✅ Delete staff
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var staff = await _context.AgencyStaff.FindAsync(id);
            if (staff == null) return NotFound();

            _context.AgencyStaff.Remove(staff);
            await _context.SaveChangesAsync();
            return Ok("Staff removed successfully");
        }
    }
}
