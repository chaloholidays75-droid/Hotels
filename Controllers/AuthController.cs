using System.Security.Claims;
using HotelAPI.Data;
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
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, AppDbContext context, ILogger<AuthController> logger)
        {
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        // ✅ WHOAMI (for debugging)
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

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }

        // ✅ REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var authResponse = await _authService.RegisterAsync(request);

                return Ok(new
                {
                    authResponse.User?.Id,
                    authResponse.User?.Email,
                    authResponse.User?.FirstName,
                    authResponse.User?.LastName,
                    authResponse.User?.Role,
                    authResponse.AccessToken,
                    authResponse.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ LOGIN — issues secure cookies
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var authResponse = await _authService.LoginAsync(request);

                // Determine expiry time based on RememberMe flag
                var rememberMe = request.RememberMe;
                var accessExpiry = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(1);
                var refreshExpiry = rememberMe ? DateTime.UtcNow.AddDays(60) : DateTime.UtcNow.AddDays(7);

                // 🍪 Set cookies
                Response.Cookies.Append("accessToken", authResponse.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = accessExpiry
                });

                Response.Cookies.Append("refreshToken", authResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = refreshExpiry
                });

                return Ok(new
                {
                    message = "Login successful",
                    userFullName = $"{authResponse.User.FirstName} {authResponse.User.LastName}",
                    userRole = authResponse.User.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ FORGOT PASSWORD
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.SendForgotPasswordEmailAsync(request);
            return Ok("Email sent if account exists.");
        }

        // ✅ RESET PASSWORD
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _authService.ResetPasswordAsync(request);
            return Ok("Password reset successful.");
        }

        // ✅ REFRESH TOKEN — uses cookies
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var cookieToken = Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(cookieToken))
                    return Unauthorized(new { message = "No refresh token found" });

                var authResponse = await _authService.RefreshTokenAsync(
                    new RefreshTokenRequest { RefreshToken = cookieToken }
                );

                Response.Cookies.Append("accessToken", authResponse.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(1)
                });

                Response.Cookies.Append("refreshToken", authResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                return Ok(new { message = "Token refreshed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ✅ LOGOUT — clear cookies
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Ok(new { message = "Logged out successfully" });
        }

        // ✅ PROTECTED ROUTE
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
