using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        // Lấy email từ cấu hình
        string email = _config["GmailSettings:Mail"];
        // Lấy mật khẩu và host từ cấu hình
        string password = _config["GmailSettings:Password"];
        // Lấy host từ cấu hình
        string host = _config["GmailSettings:Host"];

        // Tạo mail message
        var message = new MailMessage(email, to, subject, body);
        message.IsBodyHtml = true; // Cho phép gửi nội dung HTML
        // Cấu hình SMTP client
        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            // Sử dụng SSL
            EnableSsl = true,
            Credentials = new NetworkCredential(email, password)
        };
        // Gửi email
        await client.SendMailAsync(message);
    }
}
