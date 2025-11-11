using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelAPI.Models;
using HotelAPI.Data;
using System.Net.Mail;
using System.Net;
using HotelAPI.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AgencyManagementSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/agency")]
    public class AgencyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AgencyController(AppDbContext context)
        {
            _context = context;
        }
        private int? GetUserIdNullable()
        {
            var s = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrWhiteSpace(s) ? (int?)null : int.Parse(s);
        }

        // private async Task LogRecentActivityAsync(string entity, int entityId, string action, string description)
        // {
        //     int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0"); 
        //     string userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

        //     var activity = new RecentActivity
        //     {
        //         UserId = userId,
        //         UserName = userName,
        //         Entity = entity,
        //         EntityId = entityId,
        //         Action = action,
        //         Description = description,
        //         Timestamp = DateTime.UtcNow
        //     };

        //     _context.RecentActivities.Add(activity);
        //     await _context.SaveChangesAsync();
        // }

        // GET: api/agency
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agency>>> GetAgencies()
        {
            try
            {
                var agencies = await _context.Agencies
                    
                    .Include(a => a.Country)
                    .Include(a => a.City)
                    .Include(a => a.Staff)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
                return Ok(agencies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving agencies", error = ex.Message });
            }
        }
            

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Agency>>> GetActiveAgencies()
        {
            try
            {
                var agencies = await _context.Agencies
                    .AsNoTracking()
                    .Where(a => a.IsActive)
                    .Include(a => a.Country)
                    .Include(a => a.City)
                    .Include(a => a.Staff)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                return Ok(agencies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving active agencies", error = ex.Message });
            }
        }
        // GET: api/agency/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Agency>> GetAgency(int id)
        {
            var agency = await _context.Agencies.FindAsync(id);
            if (agency == null || !agency.IsActive)
                return NotFound(new { message = "Agency not found" });

            return Ok(agency);
        }

        // POST: api/agency
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        public async Task<ActionResult<AgencyRegistrationResponseDto>> CreateAgency([FromBody] AgencyRegistrationRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Agencies.AnyAsync(a => a.UserName == dto.UserName && a.IsActive))
                return Conflict(new { message = "Username already exists" });

            if (await _context.Agencies.AnyAsync(a => a.UserEmailId == dto.UserEmailId && a.IsActive))
                return Conflict(new { message = "Email already exists" });

            var agency = new Agency
            {
                AgencyName = dto.AgencyName,
                CountryId = dto.CountryId.Value,
                CityId = dto.CityId.Value,
                PostCode = dto.PostCode,
                Address = dto.Address,
                Region = dto.Region,
                Area = dto.Area,
                Website = dto.Website,
                PhoneNo = dto.PhoneNo,
                EmailId = dto.EmailId,
                BusinessCurrency = dto.BusinessCurrency,
                Title = dto.Title,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserEmailId = dto.UserEmailId,
                Designation = dto.Designation,
                MobileNo = dto.MobileNo,
                UserName = dto.UserName,
                Password = HashPassword(dto.Password),
                AcceptTerms = dto.AcceptTerms,
                SpecialRemarks = dto.SpecialRemarks,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Agencies.Add(agency);
            await _context.SaveChangesAsync();
            // await LogRecentActivityAsync("Agency", agency.Id, "CREATE", $"{agency.AgencyName} created");

             if (dto.Staff is { Count: > 0 })
    {
        var uid = GetUserIdNullable();
        var now = DateTime.UtcNow;

        // de-dup incoming list client-side: same Name+Role for this agency
        var seen = new HashSet<string>();
        var toAdd = new List<AgencyStaff>();

        foreach (var i in dto.Staff)
        {
            var role = (i.Role ?? "").Trim();
            var name = (i.Name ?? "").Trim();
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(name)) continue;

            var key = $"{agency.Id}|{role.ToLower()}|{name.ToLower()}";
            if (seen.Contains(key)) continue;
            seen.Add(key);

            // DB duplicate check too
            bool exists = await _context.AgencyStaff.AnyAsync(s =>
                s.AgencyId == agency.Id &&
                s.Role.ToLower() == role.ToLower() &&
                s.Name.ToLower() == name.ToLower());

            if (exists) continue;

            toAdd.Add(new AgencyStaff
            {
                AgencyId = agency.Id,
                Role = role,
                Designation = string.IsNullOrWhiteSpace(i.Designation) ? null : i.Designation.Trim(),
                Name = name,
                Email = string.IsNullOrWhiteSpace(i.Email) ? null : i.Email!.Trim(),
                Phone = string.IsNullOrWhiteSpace(i.Phone) ? null : i.Phone!.Trim(),
                CreatedById = uid,
                CreatedAt = now
            });
        }

        if (toAdd.Count > 0)
        {
            _context.AgencyStaff.AddRange(toAdd);
            await _context.SaveChangesAsync();
            // await LogRecentActivityAsync("AgencyStaff", agency.Id, "BULK CREATE", $"{toAdd.Count} staff added to {agency.AgencyName}");
        }
    }
            // _ = SendWelcomeEmailAsync(agency.EmailId, agency.AgencyName);

            var responseDto = new AgencyRegistrationResponseDto
            {
                Id = agency.Id,
                AgencyName = agency.AgencyName,
                CountryId = agency.CountryId,
                CityId = agency.CityId,
                PostCode = agency.PostCode,
                Address = agency.Address,
                Region = agency.Region,
                Area = agency.Area,
                Website = agency.Website,
                PhoneNo = agency.PhoneNo,
                EmailId = agency.EmailId,
                BusinessCurrency = agency.BusinessCurrency,
                Title = agency.Title,
                FirstName = agency.FirstName,
                LastName = agency.LastName,
                UserEmailId = agency.UserEmailId,
                Designation = agency.Designation,
                MobileNo = agency.MobileNo,
                UserName = agency.UserName,
                AcceptTerms = agency.AcceptTerms,
                SpecialRemarks = agency.SpecialRemarks,
                CreatedAt = agency.CreatedAt,
                UpdatedAt = agency.UpdatedAt,
                IsActive = agency.IsActive
            };

            return CreatedAtAction(nameof(GetAgency), new { id = agency.Id }, responseDto);

        }

        // PUT: api/agency/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAgency(int id, [FromBody] Agency agencyUpdate)
        {
            if (id != agencyUpdate.Id)
                return BadRequest(new { message = "ID mismatch ho rahi hai " });

            var existingAgency = await _context.Agencies.FindAsync(id);
            if (existingAgency == null || !existingAgency.IsActive)
                return NotFound(new { message = "Agency not found" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Agencies.AnyAsync(a => a.Id != id && a.UserName == agencyUpdate.UserName && a.IsActive))
                return Conflict(new { message = "Username already exists" });

            if (await _context.Agencies.AnyAsync(a => a.Id != id && a.UserEmailId == agencyUpdate.UserEmailId && a.IsActive))
                return Conflict(new { message = "Email already exists" });

            existingAgency.AgencyName = agencyUpdate.AgencyName;
            existingAgency.CountryId = agencyUpdate.CountryId;
            existingAgency.CityId = agencyUpdate.CityId;
            existingAgency.PostCode = agencyUpdate.PostCode;
            existingAgency.Address = agencyUpdate.Address;
            existingAgency.Region = agencyUpdate.Region;
            existingAgency.Area = agencyUpdate.Area;
            existingAgency.Website = agencyUpdate.Website;
            existingAgency.PhoneNo = agencyUpdate.PhoneNo;
            existingAgency.EmailId = agencyUpdate.EmailId;
            existingAgency.BusinessCurrency = agencyUpdate.BusinessCurrency;
            existingAgency.Title = agencyUpdate.Title;
            existingAgency.FirstName = agencyUpdate.FirstName;
            existingAgency.LastName = agencyUpdate.LastName;
            existingAgency.UserEmailId = agencyUpdate.UserEmailId;
            existingAgency.Designation = agencyUpdate.Designation;
            existingAgency.MobileNo = agencyUpdate.MobileNo;
            existingAgency.UserName = agencyUpdate.UserName;
            existingAgency.SpecialRemarks = agencyUpdate.SpecialRemarks;
            existingAgency.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(agencyUpdate.Password))
                existingAgency.Password = HashPassword(agencyUpdate.Password);

            await _context.SaveChangesAsync();
            // await LogRecentActivityAsync("Agency", existingAgency.Id, "UPDATE", $"{existingAgency.AgencyName} updated");
            return Ok(new
            {
                message = "Agency updated successfully",
                agency = existingAgency
            });
        }

        // DELETE: api/agency/5 (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgency(int id)
        {
            var agency = await _context.Agencies.FindAsync(id);
            if (agency == null)
                return NotFound(new { message = "Agency not found" });

            agency.IsActive = false;
            agency.UpdatedAt = DateTime.UtcNow;

            _context.Entry(agency).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            // await LogRecentActivityAsync("Agency", agency.Id, "DELETE", $"{agency.AgencyName} deactivated");

            return Ok(new { message = "Agency deactivated successfully" });
        }

        // PATCH: api/agency/5/status (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                var agency = await _context.Agencies.FindAsync(id);
                if (agency == null)
                    return NotFound(new { message = "Agency not found" });

                agency.IsActive = dto.IsActive;
                agency.UpdatedAt = DateTime.UtcNow;

                _context.Entry(agency).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                // await LogRecentActivityAsync("Agency", agency.Id, "STATUS UPDATE", $"{agency.AgencyName} status changed to {(agency.IsActive ? "Active" : "Inactive")}");
                
                return Ok(new { message = $"Agency {(dto.IsActive ? "activated" : "deactivated")} successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }



        // Helper: Check username/email
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("check-username")]
        public async Task<ActionResult<bool>> CheckUsernameExists(string username)
        {
            var exists = await _context.Agencies.AnyAsync(a => a.UserName == username && a.IsActive);
            return Ok(new { exists });
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            var exists = await _context.Agencies.AnyAsync(a => a.UserEmailId == email && a.IsActive);
            return Ok(new { exists });
        }

        // Password hashing placeholder
        private string HashPassword(string password) => password; // Replace with BCrypt in production

        private async Task SendWelcomeEmailAsync(string email, string agencyName)
        {
            try
            {
                var fromAddress = new MailAddress("chaloholidays75@gmail.com", "Chalo Holidays");
                var toAddress = new MailAddress(email, agencyName);
                var fromPassword = "nmfj cwhv gyim ctpz"; // replace securely
                const string subject = "Welcome to Chalo Holidays!";
                string body = $@"
                    <html>
                    <body>
                    <h2>Hello {agencyName},</h2>
                    <p>Welcome to <strong>Chalo Holidays!</strong></p>
                    <p>Best Regards,<br/>Chalo Holidays Team</p>
                    </body>
                    </html>";

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex.Message);
            }
        }
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("{id}/staff")]
        public async Task<IActionResult> AddStaffBulk(int id, [FromBody] List<AgencyStaffItemDto> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("No staff provided.");

            var agency = await _context.Agencies.FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
            if (agency == null) return NotFound(new { message = "Agency not found" });

            var uid = GetUserIdNullable();
            var now = DateTime.UtcNow;

            // client-side duplicate guard (in payload)
            var seen = new HashSet<string>();
            var toAdd = new List<AgencyStaff>();

            foreach (var i in items)
            {
                var role = (i.Role ?? "").Trim();
                var name = (i.Name ?? "").Trim();
                if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(name)) continue;

                var key = $"{id}|{role.ToLower()}|{name.ToLower()}";
                if (seen.Contains(key)) continue;
                seen.Add(key);

                // DB duplicate guard (same Agency + Name + Role)
                bool exists = await _context.AgencyStaff.AnyAsync(s =>
                    s.AgencyId == id &&
                    s.Role.ToLower() == role.ToLower() &&
                    s.Name.ToLower() == name.ToLower());

                if (exists) continue;

                toAdd.Add(new AgencyStaff
                {
                    AgencyId = id,
                    Role = role,
                    Designation = string.IsNullOrWhiteSpace(i.Designation) ? null : i.Designation.Trim(),
                    Name = name,
                    Email = string.IsNullOrWhiteSpace(i.Email) ? null : i.Email!.Trim(),
                    Phone = string.IsNullOrWhiteSpace(i.Phone) ? null : i.Phone!.Trim(),
                    CreatedById = uid,
                    CreatedAt = now
                });
            }

            if (toAdd.Count == 0)
                return Ok(new { added = 0, message = "No new staff added (duplicates or empty rows)" });

            _context.AgencyStaff.AddRange(toAdd);
            await _context.SaveChangesAsync();

            // await LogRecentActivityAsync("AgencyStaff", id, "BULK CREATE", $"{toAdd.Count} staff added to {agency.AgencyName}");

            return Ok(new { added = toAdd.Count });
        }
    }
}
