using System.Security.Claims;
using HotelAPI.Models.DTO;
using HotelAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            return Ok(new
            {
                Id = User.FindFirstValue("id"),
                FirstName = User.FindFirstValue("firstName"),
                LastName = User.FindFirstValue("lastName"),
                Name = User.FindFirstValue(ClaimTypes.Name)
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.SendForgotPasswordEmailAsync(request);
            return Ok("Email sent if account exists.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _authService.ResetPasswordAsync(request);
            return Ok("Password reset successful.");
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }

        // Example protected route to show logged-in user
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var fullName = User.FindFirstValue("FullName");
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new { Id = userId, Email = email, FullName = fullName, Role = role });
        }
    }
}