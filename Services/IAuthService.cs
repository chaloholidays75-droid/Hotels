using HotelAPI.Models.DTO;
namespace HotelAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> LoginWithRememberTokenAsync(string token); 
        Task RevokeRememberTokenAsync(string token);          
        Task SendForgotPasswordEmailAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(LogoutRequest request);    
        Task SendEmailAsync(string toEmail, string subject, string body);
       
    }
}