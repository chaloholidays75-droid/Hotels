using HotelAPI.Models;
namespace HotelAPI.Models.DTO
{
     public class UserDto
    {
        public int Id { get; set; }      // or int depending on your User model
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; } // if you have roles
    }
    // public class RegisterRequest
    // {
    //     public string? Email { get; set; }
    //     public string? Password { get; set; }
    //     public string? FirstName { get; set; }
    //     public string? LastName { get; set; }
    //     public string? Role { get; set; } 
    // }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public bool RememberMe { get; set; } = false;
    }

    public class ForgotPasswordRequest
    {
        public string? Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class AuthResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public UserDto? User { get; set; }  // UserDto with Id, Email, FirstName, LastName, Role
    }
}