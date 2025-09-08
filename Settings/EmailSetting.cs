namespace HotelAPI.Settings
{
    public class EmailSettings
    {
        public string SenderName { get; set; } = string.Empty;    // e.g., "Chalo Holidays"
        public string SenderEmail { get; set; } = string.Empty;   // e.g., "no-reply@chaloholidays.com"
        public string Password { get; set; } = string.Empty;      // email account password
        public string SmtpServer { get; set; } = string.Empty;    // e.g., "smtp.gmail.com"
        public int SmtpPort { get; set; } = 587;                  // usually 587 for TLS

        public string? FrontendUrl { get; set; }
    }
}
