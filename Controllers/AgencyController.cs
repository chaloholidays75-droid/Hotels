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

namespace AgencyManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgencyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AgencyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/agency
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agency>>> GetAgencies()
        {
            try
            {
                var agencies = await _context.Agencies
                    .Where(a => a.IsActive)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                return Ok(agencies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving agencies", error = ex.Message });
            }
        }

        // GET: api/agency/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Agency>> GetAgency(int id)
        {
            try
            {
                var agency = await _context.Agencies.FindAsync(id);

                if (agency == null || !agency.IsActive)
                {
                    return NotFound(new { message = "Agency not found" });
                }

                return Ok(agency);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the agency", error = ex.Message });
            }
        }

        // POST: api/agency
[HttpPost]
public async Task<ActionResult<Agency>> CreateAgency([FromBody] Agency agency)
{
    try
    {
        // Validate model
        if (!ModelState.IsValid)
        {
            return BadRequest(new { 
                message = "Invalid agency data", 
                errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            });
        }

        // Check if username already exists
        if (await _context.Agencies.AnyAsync(a => a.UserName == agency.UserName && a.IsActive))
        {
            return Conflict(new { message = "Username already exists" });
        }

        // Check if email already exists
        if (await _context.Agencies.AnyAsync(a => a.UserEmailId == agency.UserEmailId && a.IsActive))
        {
            return Conflict(new { message = "Email already exists" });
        }

        // Set timestamps
        agency.CreatedAt = DateTime.UtcNow;
        agency.UpdatedAt = DateTime.UtcNow;
        agency.IsActive = true;

        // Hash password (in a real application, use proper password hashing)
        // agency.Password = HashPassword(agency.Password);
        // agency.ConfirmPassword = null; // Clear confirm password after validation

        _context.Agencies.Add(agency);
        await _context.SaveChangesAsync();

        // Send Welcome Email asynchronously (does not block API)
        _ = Task.Run(() => SendWelcomeEmail(agency.UserEmailId, agency.FirstName));

        // Return created agency without password
        agency.Password = null;
        agency.ConfirmPassword = null;

        return CreatedAtAction(nameof(GetAgency), new { id = agency.Id }, agency);
    }
    catch (DbUpdateException ex)
    {
        return StatusCode(500, new { message = "An error occurred while creating the agency", error = ex.InnerException?.Message ?? ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An unexpected error occurred", error = ex.Message });
    }
}

        // PUT: api/agency/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAgency(int id, [FromBody] Agency agencyUpdate)
        {
            try
            {
                if (id != agencyUpdate.Id)
                {
                    return BadRequest(new { message = "ID mismatch" });
                }

                var existingAgency = await _context.Agencies.FindAsync(id);
                if (existingAgency == null || !existingAgency.IsActive)
                {
                    return NotFound(new { message = "Agency not found" });
                }

                // Validate model
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid agency data",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                // Check if username already exists (excluding current agency)
                if (await _context.Agencies.AnyAsync(a => a.Id != id && a.UserName == agencyUpdate.UserName && a.IsActive))
                {
                    return Conflict(new { message = "Username already exists" });
                }

                // Check if email already exists (excluding current agency)
                if (await _context.Agencies.AnyAsync(a => a.Id != id && a.UserEmailId == agencyUpdate.UserEmailId && a.IsActive))
                {
                    return Conflict(new { message = "Email already exists" });
                }

                // Update properties
                existingAgency.AgencyName = agencyUpdate.AgencyName;
                existingAgency.Country = agencyUpdate.Country;
                existingAgency.City = agencyUpdate.City;
                existingAgency.PostCode = agencyUpdate.PostCode;
                existingAgency.Address = agencyUpdate.Address;
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

                // Only update password if provided
                if (!string.IsNullOrEmpty(agencyUpdate.Password))
                {
                    // Hash new password (in a real application)
                    // existingAgency.Password = HashPassword(agencyUpdate.Password);
                    existingAgency.Password = agencyUpdate.Password;
                }

                _context.Entry(existingAgency).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AgencyExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the agency", error = ex.Message });
            }
        }

        // DELETE: api/agency/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgency(int id)
        {
            try
            {
                var agency = await _context.Agencies.FindAsync(id);
                if (agency == null)
                {
                    return NotFound(new { message = "Agency not found" });
                }

                // Soft delete (set IsActive to false)
                agency.IsActive = false;
                agency.UpdatedAt = DateTime.UtcNow;

                _context.Entry(agency).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the agency", error = ex.Message });
            }
        }

        // PATCH: api/agency/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateAgencyStatus(int id, [FromBody] bool isActive)
        {
            try
            {
                var agency = await _context.Agencies.FindAsync(id);
                if (agency == null)
                {
                    return NotFound(new { message = "Agency not found" });
                }

                agency.IsActive = isActive;
                agency.UpdatedAt = DateTime.UtcNow;

                _context.Entry(agency).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Agency {(isActive ? "activated" : "deactivated")} successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating agency status", error = ex.Message });
            }
        }

        // GET: api/agency/check-username?username=test
        [HttpGet("check-username")]
        public async Task<ActionResult<bool>> CheckUsernameExists(string username)
        {
            try
            {
                var exists = await _context.Agencies
                    .AnyAsync(a => a.UserName == username && a.IsActive);

                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking username", error = ex.Message });
            }
        }

        // GET: api/agency/check-email?email=test@example.com
        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            try
            {
                var exists = await _context.Agencies
                    .AnyAsync(a => a.UserEmailId == email && a.IsActive);

                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking email", error = ex.Message });
            }
        }

        private bool AgencyExists(int id)
        {
            return _context.Agencies.Any(e => e.Id == id && e.IsActive);
        }

        // Helper method for password hashing (implement properly in production)
        private string HashPassword(string password)
        {
            // In a real application, use a proper hashing algorithm like BCrypt
            // For example: return BCrypt.Net.BCrypt.HashPassword(password);
            return password; // This is just a placeholder - NEVER store passwords in plain text
        }

private void SendWelcomeEmail(string email, string firstName)
{
    try
    {
        var fromAddress = new MailAddress("chaloholidays75@gmail.com", "Chalo Holidays");
        var toAddress = new MailAddress(email, firstName);
        const string fromPassword = "nmfj cwhv gyim ctpz"; // Use secure storage
        const string subject = "Welcome to Chalo Holidays!";
        string body = $"Hello {firstName},\n\n" +
                      "Welcome to Chalo Holidays! We are excited to have you onboard.\n\n" +
                      "Best Regards,\nChalo Holidays Team";

        using (var smtp = new SmtpClient
        {
            Host = "smtp.your-email-provider.com", // e.g., smtp.gmail.com
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        })
        using (var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body
        })
        {
            smtp.Send(message);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to send welcome email: " + ex.Message);
        // We don’t block registration if email fails
    }
}


    }

    
}