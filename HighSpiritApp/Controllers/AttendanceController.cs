using HighSpiritApp.Helpers;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace HighSpiritApp.Controllers
{
    /// <summary>
    /// Attendance controller - QR-based punch in/out and attendance management
    /// </summary>
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ICustomerService _customerService;
        private readonly IEmailService _emailService;
        private readonly string _qrSecretKey;

        public AttendanceController(IAttendanceService attendanceService, ICustomerService customerService, IEmailService emailService, IConfiguration config)
        {
            _attendanceService = attendanceService;
            _customerService = customerService;
            _qrSecretKey = config["QRSecretKey"] ?? "DefaultFallbackKey2026";
            _emailService = emailService;
        }

        /// <summary>
        /// Admin page - View attendance records with date filter
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Index(DateTime? date, DateTime? fromDate, DateTime? toDate)
        {
            var selectedDate = date.HasValue ? DateOnly.FromDateTime(date.Value) : DateOnly.FromDateTime(DateTime.Today);

            IEnumerable<Models.Attendance.AttendanceRecord> records;

            if (fromDate.HasValue && toDate.HasValue)
            {
                records = await _attendanceService.GetAttendanceByRangeAsync(
                    DateOnly.FromDateTime(fromDate.Value),
                    DateOnly.FromDateTime(toDate.Value));
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
                ViewBag.FilterMode = "range";
            }
            else
            {
                records = await _attendanceService.GetAttendanceByDateAsync(selectedDate);
                ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
                ViewBag.FilterMode = "single";
            }

            var summary = await _attendanceService.GetTodaySummaryAsync();

            ViewBag.TotalCheckIns = summary.TotalCheckIns;
            ViewBag.CurrentlyInGym = summary.CurrentlyInGym;
            ViewBag.AlreadyLeft = summary.AlreadyLeft;
            ViewBag.Records = records;

            return View();
        }

        /// <summary>
        /// Admin page - QR Scanner using device camera to scan customer QR codes
        /// </summary>
        [Authorize]
        public IActionResult Scanner()
        {
            return View();
        }

        /// <summary>
        /// Admin page - Generate and display QR code for a specific customer
        /// </summary>
        [Authorize]
        public async Task<IActionResult> CustomerQR(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
            {
                TempData["error"] = "Customer not found.";
                return RedirectToAction("Index", "Customers");
            }

            // QR encodes an encrypted token (not the raw ID)
            var qrContent = QREncryptionHelper.Encrypt(id, _qrSecretKey);

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(20);
            var qrBase64 = Convert.ToBase64String(qrBytes);

            ViewBag.QRCodeImage = $"data:image/png;base64,{qrBase64}";
            ViewBag.Customer = customer;

            return View();
        }

        /// <summary>
        /// API endpoint - Process punch in/out (called via AJAX from Scanner page)
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProcessPunch([FromBody] PunchRequest request)
        {
            int customerId;

            if (request != null && !string.IsNullOrWhiteSpace(request.QRCode))
            {
                // Decrypt QR code token to get customer ID
                var decrypted = QREncryptionHelper.Decrypt(request.QRCode, _qrSecretKey);
                if (decrypted == null)
                {
                    return Json(new { success = false, message = "Invalid or tampered QR code.", punchType = "Error" });
                }
                customerId = decrypted.Value;
            }
            else if (request != null && request.CustomerId > 0)
            {
                // Manual ID entry fallback
                customerId = request.CustomerId;
            }
            else
            {
                return Json(new { success = false, message = "Invalid QR code scanned.", punchType = "Error" });
            }

            var result = await _attendanceService.PunchAsync(customerId);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                punchType = result.PunchType,
                customerName = result.CustomerName,
                time = result.Time?.ToString("hh:mm tt")
            });
        }

        /// <summary>
        /// Admin page - Bulk QR generation and email sending
        /// </summary>
        [Authorize]
        public async Task<IActionResult> BulkQR()
        {
            var customers = (await _customerService.GetAllAsync()).ToList();
            var today = DateTime.Today;

            var activeCustomers = customers
                .Where(c => c.Memberships != null && c.Memberships.Any() &&
                    c.Memberships.OrderByDescending(m => m.StartDate).First().ExpireDate >= today)
                .OrderBy(c => c.FullName).ToList();

            var allCustomers = customers.OrderBy(c => c.FullName).ToList();

            ViewBag.ActiveCustomers = activeCustomers;
            ViewBag.AllCustomers = allCustomers;
            return View();
        }

        /// <summary>
        /// API endpoint - Generate QR code PNG bytes for a customer
        /// </summary>
        private byte[] GenerateQRBytes(int customerId)
        {
            var encrypted = QREncryptionHelper.Encrypt(customerId, _qrSecretKey);
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(encrypted, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        /// <summary>
        /// API endpoint - Send QR codes via email to selected customers (AJAX)
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendBulkEmail([FromBody] BulkEmailRequest request)
        {
            if (request == null || request.CustomerIds == null || !request.CustomerIds.Any())
            {
                return Json(new { success = false, message = "No customers selected." });
            }

            var emailRequests = new List<QREmailRequest>();
            var skipped = new List<string>();
            var alreadySent = new List<string>();

            foreach (var id in request.CustomerIds)
            {
                var customer = await _customerService.GetByIdAsync(id);
                if (customer == null) continue;

                if (string.IsNullOrWhiteSpace(customer.Email))
                {
                    skipped.Add(customer.FullName + " (no email)");
                    continue;
                }

                if (request.SkipAlreadySent && customer.QREmailSentAt.HasValue)
                {
                    alreadySent.Add(customer.FullName);
                    continue;
                }

                var qrBytes = GenerateQRBytes(id);
                emailRequests.Add(new QREmailRequest
                {
                    CustomerId = id,
                    CustomerName = customer.FullName,
                    Email = customer.Email,
                    QRImageBytes = qrBytes
                });
            }

            if (!emailRequests.Any())
            {
                var msg = alreadySent.Any()
                    ? $"All selected customers already have QR emails sent. ({alreadySent.Count} skipped)"
                    : "No customers with email addresses found.";
                return Json(new { success = false, message = msg, skipped, alreadySent });
            }

            var result = await _emailService.SendBulkQRCodesAsync(emailRequests);

            // Mark successfully sent customers
            if (result.Sent > 0)
            {
                var sentIds = emailRequests
                    .Where(r => !result.Errors.Any(e => e.Contains(r.Email)))
                    .Select(r => r.CustomerId);
                await _customerService.MarkQREmailSentAsync(sentIds);
            }

            return Json(new
            {
                success = result.Sent > 0,
                message = $"Sent: {result.Sent}, Failed: {result.Failed}, Skipped (no email): {skipped.Count}, Already sent: {alreadySent.Count}",
                sent = result.Sent,
                failed = result.Failed,
                skippedCount = skipped.Count,
                alreadySentCount = alreadySent.Count,
                skipped,
                alreadySent,
                errors = result.Errors
            });
        }
    }

    public class PunchRequest
    {
        public string? QRCode { get; set; }
        public int CustomerId { get; set; }
    }

    public class BulkEmailRequest
    {
        public List<int> CustomerIds { get; set; } = new();
        public bool SkipAlreadySent { get; set; } = true;
    }
}
