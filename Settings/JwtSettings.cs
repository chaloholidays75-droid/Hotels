namespace HotelAPI.Settings
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;                   // secret key
        public string Issuer { get; set; } = string.Empty;               // e.g., "yourapp.com"
        public string Audience { get; set; } = string.Empty;             // e.g., "yourapp.com"
        public int AccessTokenExpiryMinutes { get; set; } = 30;          // token expiry
        public int RefreshTokenExpiryDays { get; set; } = 7;             // refresh token expiry
    }
}
