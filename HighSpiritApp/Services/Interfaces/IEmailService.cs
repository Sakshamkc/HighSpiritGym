namespace HighSpiritApp.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendQRCodeEmailAsync(string toEmail, string customerName, byte[] qrImageBytes);
        Task<BulkEmailResult> SendBulkQRCodesAsync(IEnumerable<QREmailRequest> requests);
    }

    public class QREmailRequest
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public byte[] QRImageBytes { get; set; } = Array.Empty<byte>();
    }

    public class BulkEmailResult
    {
        public int TotalRequested { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
