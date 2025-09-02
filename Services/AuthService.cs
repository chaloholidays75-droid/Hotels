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
using HotelAPI.Settings;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailSettings _emailSettings;

    public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings, IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _emailSettings = emailSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (_context.Users.Any(u => u.Email == request.Email))
            throw new Exception("Email already exists");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = "Employee"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var authResponse = await GenerateTokensAsync(user);

        await SendEmailAsync(user.Email, "Registration Successful", $"Welcome {user.FirstName}! Your account is registered.");

        return authResponse;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        var authResponse = await GenerateTokensAsync(user);

        await SendEmailAsync(user.Email, "Login Alert", $"You logged in successfully as {user.FirstName} {user.LastName}.");

        return authResponse;
    }

    public async Task SendForgotPasswordEmailAsync(ForgotPasswordRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
        if (user == null) return;  // Silent fail for security

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.ResetPasswordToken = token;
        user.ResetPasswordExpiry = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        var resetLink = $"https://yourfrontend.com/reset-password?token={token}";
        await SendEmailAsync(user.Email, "Forgot Password", $"Click to reset: {resetLink}");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.ResetPasswordToken == request.Token && u.ResetPasswordExpiry > DateTime.UtcNow);
        if (user == null) throw new Exception("Invalid or expired token");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetPasswordToken = null;
        user.ResetPasswordExpiry = null;
        await _context.SaveChangesAsync();

        await SendEmailAsync(user.Email, "Password Reset Successful", "Your password has been reset.");
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshToken = _context.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken && !rt.IsRevoked && rt.ExpiryDate > DateTime.UtcNow);
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
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", $"{user.FirstName} {user.LastName}")
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
}