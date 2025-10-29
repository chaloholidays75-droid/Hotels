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

        // ------------------------------------------------------------
        // ✅ WHOAMI — For Debugging
        // ------------------------------------------------------------
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            return Ok(new
            {
                Id = User.FindFirstValue("id"),
                FirstName = User.FindFirstValue("firstName"),
                LastName = User.FindFirstValue("lastName"),
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        // ------------------------------------------------------------
        // ✅ REGISTER
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // ✅ LOGIN — Sets Secure Cookies + Returns JWT
        // ------------------------------------------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var auth = await _authService.LoginAsync(request);

                var cookieOptsShort = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    
                    Expires = DateTime.UtcNow.AddMinutes(30)
                };
                var cookieOptsLong = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                Response.Cookies.Append("accessToken", auth.AccessToken, cookieOptsShort);
                Response.Cookies.Append("refreshToken", auth.RefreshToken, cookieOptsLong);

                if (request.RememberMe && !string.IsNullOrEmpty(auth.RememberToken))
                {
                    var rememberOpts = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        
                        Expires = DateTime.UtcNow.AddDays(30)
                    };
                    Response.Cookies.Append("rememberToken", auth.RememberToken, rememberOpts);
                }

                return Ok(new
                {
                    message = "Login successful",
                    userFullName = $"{auth.User.FirstName} {auth.User.LastName}",
                    userRole = auth.User.Role,
                    accessToken = auth.AccessToken,
                    refreshToken = auth.RefreshToken,
                    rememberToken = auth.RememberToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ------------------------------------------------------------
        // ✅ AUTO-LOGIN — via Remember Token Cookie
        // ------------------------------------------------------------
        [HttpPost("auto-login")]
        public async Task<IActionResult> AutoLogin()
        {
            var token = Request.Cookies["rememberToken"];
            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { message = "No remember token found" });

            try
            {
                var auth = await _authService.LoginWithRememberTokenAsync(token);

                var cookieOptsShort = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                   
                    Expires = DateTime.UtcNow.AddMinutes(30)
                };
                var cookieOptsLong = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                   
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                var cookieOptsRemember = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                   
                    Expires = DateTime.UtcNow.AddDays(30)
                };

                Response.Cookies.Append("accessToken", auth.AccessToken, cookieOptsShort);
                Response.Cookies.Append("refreshToken", auth.RefreshToken, cookieOptsLong);
                Response.Cookies.Append("rememberToken", auth.RememberToken, cookieOptsRemember);

                return Ok(new
                {
                    message = "Auto-login successful",
                    userFullName = $"{auth.User.FirstName} {auth.User.LastName}",
                    userRole = auth.User.Role,
                    accessToken = auth.AccessToken,
                    refreshToken = auth.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-login failed");
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ------------------------------------------------------------
        // ✅ REFRESH TOKEN — via Cookie or Body
        // ------------------------------------------------------------
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? body)
        {
            try
            {
                var token = body?.RefreshToken ?? Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(token))
                    return Unauthorized(new { message = "No refresh token found" });

                var auth = await _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = token });

                var cookieOptsShort = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                   
                    Expires = DateTime.UtcNow.AddMinutes(30)
                };
                var cookieOptsLong = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                Response.Cookies.Append("accessToken", auth.AccessToken, cookieOptsShort);
                Response.Cookies.Append("refreshToken", auth.RefreshToken, cookieOptsLong);

                return Ok(new
                {
                    message = "Token refreshed successfully",
                    accessToken = auth.AccessToken,
                    refreshToken = auth.RefreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ------------------------------------------------------------
        // ✅ LOGOUT — Clears Cookies + Revokes Tokens
        // ------------------------------------------------------------
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
        {
            try
            {
                // revoke refresh token if provided
                if (request != null && !string.IsNullOrEmpty(request.RefreshToken))
                    await _authService.LogoutAsync(request);

                var remember = Request.Cookies["rememberToken"];
                if (!string.IsNullOrEmpty(remember))
                    await _authService.RevokeRememberTokenAsync(remember);

                var opts = new CookieOptions
                {
                    Expires = DateTime.UnixEpoch,
                    Domain = ".chaloholidayonline.com",
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    HttpOnly = true
                };
                Response.Cookies.Append("accessToken", "", opts);
                Response.Cookies.Append("refreshToken", "", opts);
                Response.Cookies.Append("rememberToken", "", opts);

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ------------------------------------------------------------
        // ✅ PASSWORD FLOWS
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // ✅ AUTHORIZED PROFILE
        // ------------------------------------------------------------
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
