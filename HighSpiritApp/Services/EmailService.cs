using HighSpiritApp.Services.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Utils;

namespace HighSpiritApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendQRCodeEmailAsync(string toEmail, string customerName, byte[] qrImageBytes)
        {
            try
            {
                var smtpHost = _config["SmtpSettings:Host"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["SmtpSettings:Port"] ?? "587");
                var smtpUser = _config["SmtpSettings:Username"] ?? "";
                var smtpPass = _config["SmtpSettings:Password"] ?? "";
                var fromName = _config["SmtpSettings:FromName"] ?? "High Spirit Gym";
                var fromEmail = _config["SmtpSettings:FromEmail"] ?? smtpUser;

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    _logger.LogError("SMTP credentials not configured in appsettings.json");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(customerName, toEmail));
                message.Subject = "Your High Spirit Gym QR Code";

                var builder = new BodyBuilder();
                builder.TextBody = $"Hi {customerName},\n\n" +
                    "Welcome to High Spirit Gym!\n" +
                    "Your personal QR code is attached to this email.\n" +
                    "Simply show this QR code at the reception when you arrive.\n\n" +
                    "Thank you,\nHigh Spirit Gym Team";

                // Embed QR image inline for the HTML template
                var qrImage = builder.LinkedResources.Add("qr-code.png", qrImageBytes, ContentType.Parse("image/png"));
                qrImage.ContentId = MimeUtils.GenerateMessageId();

                builder.HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0; padding:0; background:#f4f6f9; font-family: Segoe UI, Arial, sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f6f9; padding:40px 0;'>
  <tr><td align='center'>
    <table width='520' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,0.08);'>
      <!-- Header -->
      <tr>
        <td style='background:linear-gradient(135deg,#3b82f6,#1d4ed8); padding:32px 40px; text-align:center;'>
          <div style='font-size:28px; font-weight:800; color:#ffffff; letter-spacing:1px;'>HIGH SPIRIT GYM</div>
          <div style='font-size:13px; color:rgba(255,255,255,0.8); margin-top:4px; letter-spacing:0.5px;'>Your Fitness Journey Starts Here</div>
        </td>
      </tr>
      <!-- Body -->
      <tr>
        <td style='padding:36px 40px 20px;'>
          <p style='font-size:16px; color:#1e293b; margin:0 0 8px;'>Hello <strong>{customerName}</strong>,</p>
          <p style='font-size:14px; color:#475569; line-height:1.7; margin:0 0 24px;'>
            Welcome! Your personal QR code for gym attendance is ready. Simply show this QR code at the reception desk when you arrive — no need for cards or manual sign‑ins.
          </p>
          <!-- QR Code Box -->
          <table width='100%' cellpadding='0' cellspacing='0'>
            <tr><td align='center'>
              <div style='background:#f8fafc; border:2px dashed #cbd5e1; border-radius:16px; padding:24px; display:inline-block;'>
                <img src='cid:{qrImage.ContentId}' width='180' height='180' alt='Your QR Code' style='display:block; margin:0 auto;' />
                <p style='font-size:12px; color:#64748b; margin:12px 0 0; text-align:center;'>Scan at gym reception</p>
              </div>
            </td></tr>
          </table>
        </td>
      </tr>
      <!-- Instructions -->
      <tr>
        <td style='padding:12px 40px 32px;'>
          <div style='background:#eff6ff; border-radius:12px; padding:16px 20px; border-left:4px solid #3b82f6;'>
            <p style='font-size:13px; font-weight:600; color:#1e40af; margin:0 0 8px;'>How to use:</p>
            <p style='font-size:13px; color:#334155; margin:0; line-height:1.7;'>
              1. Save or screenshot this QR code on your phone<br/>
              2. Show it at the reception when you enter the gym<br/>
              3. Show it again when you leave to record your session
            </p>
          </div>
        </td>
      </tr>
      <!-- Footer -->
      <tr>
        <td style='background:#f8fafc; padding:20px 40px; border-top:1px solid #e2e8f0; text-align:center;'>
          <p style='font-size:12px; color:#94a3b8; margin:0;'>This is a personalized QR code — please do not share it with others.</p>
          <p style='font-size:12px; color:#94a3b8; margin:8px 0 0;'>&copy; {DateTime.Now.Year} High Spirit Gym. All rights reserved.</p>
        </td>
      </tr>
    </table>
  </td></tr>
</table>
</body>
</html>";

                // Also attach as downloadable file
                builder.Attachments.Add($"QR-{customerName.Replace(" ", "_")}.png", qrImageBytes, ContentType.Parse("image/png"));

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("QR email sent to {Email} for {Customer}", toEmail, customerName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send QR email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<BulkEmailResult> SendBulkQRCodesAsync(IEnumerable<QREmailRequest> requests)
        {
            var result = new BulkEmailResult();
            var requestList = requests.ToList();
            result.TotalRequested = requestList.Count;

            foreach (var req in requestList)
            {
                var sent = await SendQRCodeEmailAsync(req.Email, req.CustomerName, req.QRImageBytes);
                if (sent)
                {
                    result.Sent++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add($"Failed to send to {req.CustomerName} ({req.Email})");
                }

                // Small delay between emails to avoid rate limiting
                await Task.Delay(500);
            }

            return result;
        }
    }
}
