using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using MailKit.Net.Smtp;
using MimeKit;
using HotelAPI.Models.DTO;
using HotelAPI.Data;
using HotelAPI.Models;
using BCrypt.Net;
using HotelAPI.Settings;
using Microsoft.EntityFrameworkCore;
namespace HotelAPI.Services
{

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings, IOptions<EmailSettings> emailSettings, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                throw new Exception("Email already exists");

            // Ensure Role is set from request, otherwise throw
            if (string.IsNullOrWhiteSpace(request.Role))
                throw new Exception("Role is required");

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.Role.Trim(),  // Use exactly what comes from request
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Debug log to confirm
            Console.WriteLine($"[Register] Saving user: Email={user.Email}, Role={user.Role}");

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var authResponse = await GenerateTokensAsync(user);

            await SendEmailAsync(user.Email, "Registration Successful",
                $"Welcome {user.FirstName}! Your account is registered as {user.Role}.");

            return authResponse;
        }



        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
            .Where(u => u.Email == request.Email)
            .Select(u => new User
            {
                Id = u.Id,
                Email = u.Email,
                PasswordHash = u.PasswordHash,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role
            })
            .SingleOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            var authResponse = await GenerateTokensAsync(user);

            await SendEmailAsync(user.Email, "Login Alert", $"You logged in successfully as {user.FirstName} {user.LastName}.");

            return authResponse;
        }
        public async Task LogoutAsync(LogoutRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                throw new Exception("Refresh token is required");

            // Find the refresh token
            var refreshToken = await _context.RefreshTokens
                .SingleOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }


        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string output = input.Replace("-", "+").Replace("_", "/");
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            return Convert.FromBase64String(output);
        }


        public async Task SendForgotPasswordEmailAsync(ForgotPasswordRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (user == null) return; // Silent fail for security

            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Base64UrlEncode(tokenBytes);

            user.ResetPasswordToken = token;
            user.ResetPasswordExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            // Use frontend URL from settings
            // var frontendUrl = _emailSettings.FrontendUrl ?? "https://hotels-ui-obxn.onrender.com";
            // var resetLink = $"{frontendUrl}/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";
            // var resetLink = $"http://localhost:5173/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";
            var resetLink = $"https://backend.chaloholidayonline.com/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";

            await SendEmailAsync(user.Email, "Forgot Password", $"Click to reset: {resetLink}");
        }
        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.ResetPasswordToken == request.Token && u.ResetPasswordExpiry > DateTime.UtcNow);
            if (user == null) throw new Exception("Invalid or expired token");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;
            await _context.SaveChangesAsync();

            await SendEmailAsync(user.Email, "Password Reset Successful", "Your password has been reset.");
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var refreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked && rt.ExpiryDate > DateTime.UtcNow);
            if (refreshToken == null) throw new Exception("Invalid refresh token");

            var user = _context.Users.Find(refreshToken.UserId);
            return await GenerateTokensAsync(user, true);  // Revoke old refresh token
        }

        private async Task<AuthResponse> GenerateTokensAsync(User user, bool revokeOld = false)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", $"{user.FirstName} {user.LastName}"),

            new Claim("id", user.Id.ToString()),
            new Claim("firstName", user.FirstName ?? ""),
            new Claim("lastName", user.LastName ?? "")
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var accessToken = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                signingCredentials: creds
            );

            var refreshTokenStr = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenStr,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            };

            if (revokeOld)
            {
                var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id && !rt.IsRevoked);
                foreach (var old in oldTokens) old.IsRevoked = true;
            }

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
                RefreshToken = refreshTokenStr,
                User = new UserDto { Id = user.Id, Email = user.Email, FirstName = user.FirstName, LastName = user.LastName, Role = user.Role }
            };
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            return await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role
                })
                .ToListAsync();
        }
    }
}
