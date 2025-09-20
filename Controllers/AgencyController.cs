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

        // GET: api/agency
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agency>>> GetAgencies()
        {
            try
            {
                var agencies = await _context.Agencies
                    .Where(a => a.IsActive)
                    .Include(a => a.Country)
                    .Include(a => a.City)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
                return Ok(agencies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving agencies", error = ex.Message });
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Agencies.Add(agency);
            await _context.SaveChangesAsync();

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
                return BadRequest(new { message = "ID mismatch" });

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
            existingAgency.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(agencyUpdate.Password))
                existingAgency.Password = HashPassword(agencyUpdate.Password);

            await _context.SaveChangesAsync();
            return NoContent();
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

            return NoContent();
        }

        // PATCH: api/agency/5/status (Admin only)
[HttpPatch("{id}/status")]
public async Task<IActionResult> UpdateAgencyStatus(int id, [FromBody] UpdateAgencyStatusDto dto)
{
    if (dto == null)
        return BadRequest(new { message = "Invalid request body" });

    if (id != dto.AgencyId)
        return BadRequest(new { message = "Route id and body AgencyId do not match" });

    var agency = await _context.Agencies.FindAsync(id);
    if (agency == null)
        return NotFound(new { message = "Agency not found" });

    agency.IsActive = dto.IsActive;
    agency.UpdatedAt = DateTime.UtcNow;

    _context.Entry(agency).State = EntityState.Modified;
    await _context.SaveChangesAsync();

    return Ok(new { message = $"Agency {(dto.IsActive ? "activated" : "deactivated")} successfully" });
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
    }
}
