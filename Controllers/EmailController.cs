using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace HotelAPI.Controllers
{
    [ApiController]
    [Route("api/email")]
    public class EmailController : ControllerBase
    {
        [HttpPost("send-invoice")]
        public async Task<IActionResult> SendInvoiceEmail([FromForm] IFormFile file, [FromForm] string email, [FromForm] int bookingId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Attachment missing");

            string subject = $"Invoice for Booking #{bookingId}";
            string body = "Dear Partner,\n\nPlease find attached the invoice.\n\nRegards,\nChalo Holiday";

            using var message = new MailMessage();
            message.From = new MailAddress("no-reply@chaloholidayonline.com", "Chalo Holiday");
            message.To.Add(email);
            message.Subject = subject;
            message.Body = body;

            // Add attachment
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            message.Attachments.Add(new Attachment(ms, file.FileName, file.ContentType));

            using var smtp = new SmtpClient("smtp.gmail.com", 587); // change to your SMTP
            smtp.Credentials = new NetworkCredential("your-email@gmail.com", "your-password-or-app-password");
            smtp.EnableSsl = true;

            await smtp.SendMailAsync(message);

            return Ok(new { success = true, message = "Email sent" });
        }
    }
}
