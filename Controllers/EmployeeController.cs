// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using HotelAPI.Data;
// using HotelAPI.Models;
// using Microsoft.AspNetCore.Authorization;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;

// namespace HotelAPI.Controllers
// {
//     [Authorize]
//     [ApiController]
//     [Route("api/[controller]")]
//     public class EmployeeController : ControllerBase
//     {
//         private readonly AppDbContext _context;

//         public EmployeeController(AppDbContext context)
//         {
//             _context = context;
//         }

//         // GET: api/employee
//         [HttpGet]
//         public async Task<ActionResult<IEnumerable<Agency>>> GetEmployees()
//         {
//             var employees = await _context.Agencies
//                 .Where(a => a.IsActive && a.Designation == "Employee") // only active employees
//                 .OrderBy(a => a.FirstName)
//                 .ToListAsync();

//             return Ok(employees);
//         }

//         // GET: api/employee/5
//         [HttpGet("{id}")]
//         public async Task<ActionResult<Agency>> GetEmployee(int id)
//         {
//             var employee = await _context.Agencies
//                 .FirstOrDefaultAsync(a => a.Id == id && a.IsActive && a.Designation == "Employee");

//             if (employee == null)
//                 return NotFound(new { message = "Employee not found" });

//             return Ok(employee);
//         }

//         // GET: api/employee
//         [HttpGet]
//         [Authorize(Roles = "Admin")] // Only admin can see all employees
//         public async Task<ActionResult<IEnumerable<Employee>>> GetAllEmployees()
//         {
//             var employees = await _context.Employees
//                                           .Where(e => e.IsActive)
//                                           .ToListAsync();
//             return Ok(employees);
//         }

//         // GET: api/employee/me
//         [HttpGet("me")]
//         public async Task<ActionResult<Employee>> GetMyInfo()
//         {
//             var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
//             if (string.IsNullOrEmpty(userId))
//                 return Unauthorized();

//             var employee = await _context.Employees
//                                          .FirstOrDefaultAsync(e => e.Id.ToString() == userId && e.IsActive);

//             if (employee == null)
//                 return NotFound(new { message = "Employee not found" });

//             return Ok(employee);
//         }

//         // PUT: api/employee/{id} -> Only Admin
//         [HttpPut("{id}")]
//         [Authorize(Roles = "Admin")]
//         public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee updatedEmployee)
//         {
//             if (id != updatedEmployee.Id)
//                 return BadRequest(new { message = "ID mismatch" });

//             var employee = await _context.Employees.FindAsync(id);
//             if (employee == null)
//                 return NotFound(new { message = "Employee not found" });

//             // Update fields
//             employee.FirstName = updatedEmployee.FirstName;
//             employee.LastName = updatedEmployee.LastName;
//             employee.Email = updatedEmployee.Email;
//             employee.Role = updatedEmployee.Role;
//             employee.UpdatedAt = DateTime.UtcNow;

//             await _context.SaveChangesAsync();
//             return NoContent();
//         }

//         // PATCH: api/employee/{id}/status -> Only Admin
//         [HttpPatch("{id}/status")]
//         [Authorize(Roles = "Admin")]
//         public async Task<IActionResult> UpdateEmployeeStatus(int id, [FromBody] bool isActive)
//         {
//             var employee = await _context.Employees.FindAsync(id);
//             if (employee == null)
//                 return NotFound(new { message = "Employee not found" });

//             employee.IsActive = isActive;
//             employee.UpdatedAt = DateTime.UtcNow;

//             await _context.SaveChangesAsync();
//             return Ok(new { message = $"Employee {(isActive ? "activated" : "deactivated")} successfully" });
//         }
//     }
// }

//     }
// }
