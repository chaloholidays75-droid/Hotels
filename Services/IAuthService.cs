using HotelAPI.Models.DTO;
namespace HotelAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task SendForgotPasswordEmailAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}