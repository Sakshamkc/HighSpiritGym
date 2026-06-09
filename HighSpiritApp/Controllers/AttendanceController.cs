using HighSpiritApp.DataContext;
using HighSpiritApp.Hubs;
using HighSpiritApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HighSpiritApp.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly GymDbContext _context;
        private readonly IHubContext<AttendanceHub> _hubContext;

        // Nepal is UTC+5:45
        private static readonly TimeSpan NepalOffset = new TimeSpan(5, 45, 0);
        private static DateTime NepalNow => DateTime.UtcNow.Add(NepalOffset);
        private static DateTime NepalToday => NepalNow.Date;

        public AttendanceController(GymDbContext context, IHubContext<AttendanceHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: /Attendance - Admin attendance list (requires login)
        [Authorize]
        public async Task<IActionResult> Index(string? search, DateTime? date)
        {
            var targetDate = date ?? NepalToday;
            var query = _context.Attendances
                .Where(a => a.CheckInTime.Date == targetDate.Date)
                .OrderByDescending(a => a.CheckInTime)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.CustomerName.Contains(search));
            }

            var attendances = await query.ToListAsync();
            ViewBag.Search = search;
            ViewBag.Date = targetDate.ToString("yyyy-MM-dd");
            ViewBag.TotalToday = attendances.Count;
            ViewBag.CurrentlyIn = attendances.Count(a => a.CheckOutTime == null);
            ViewBag.PunchedOut = attendances.Count(a => a.CheckOutTime != null);
            return View(attendances);
        }

        // GET: /Attendance/Display - iPad display page (public, no login needed)
        [AllowAnonymous]
        public IActionResult Display()
        {
            return View();
        }

        // GET: /Attendance/CheckIn - Customer check-in page (public, opened via QR scan)
        [AllowAnonymous]
        public IActionResult CheckIn()
        {
            return View();
        }

        // POST: /Attendance/PunchIn - API for customer punch-in
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PunchIn([FromBody] PunchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
                return Json(new { success = false, message = "Please enter your name." });

            var name = request.Name.Trim();
            var phone = request.Phone?.Trim();
            var hasPhone = !string.IsNullOrWhiteSpace(phone);

            // Check if already punched in today (not yet punched out)
            var today = NepalToday;
            Attendance? existing;
            if (hasPhone)
            {
                existing = await _context.Attendances
                    .Where(a => a.Notes == phone
                             && a.CheckInTime.Date == today
                             && a.CheckOutTime == null)
                    .FirstOrDefaultAsync();
            }
            else
            {
                existing = await _context.Attendances
                    .Where(a => a.CustomerName.ToLower() == name.ToLower()
                             && a.CheckInTime.Date == today
                             && a.CheckOutTime == null)
                    .FirstOrDefaultAsync();
            }

            if (existing != null)
            {
                return Json(new { success = false, message = "You are already checked in. Please punch out first.", alreadyIn = true });
            }

            // Find customer by phone or name (optional - for linking)
            var customer = hasPhone
                ? await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone)
                : await _context.Customers.FirstOrDefaultAsync(c => c.FullName.ToLower() == name.ToLower());

            var nepalNow = NepalNow;
            var attendance = new Attendance
            {
                CustomerID = customer?.CustomerID ?? 0,
                CustomerName = customer?.FullName ?? name,
                CheckInTime = nepalNow,
                CheckOutTime = null,
                Notes = phone // Store phone in Notes for identification (may be null)
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            // Notify iPad display via SignalR
            await _hubContext.Clients.Group("Display").SendAsync("PunchIn", new
            {
                name = attendance.CustomerName,
                time = attendance.CheckInTime.ToString("hh:mm tt")
            });

            return Json(new { success = true, message = $"Punched in at {attendance.CheckInTime:hh:mm tt}. Enjoy your workout!", type = "in" });
        }

        // POST: /Attendance/PunchOut - API for customer punch-out
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PunchOut([FromBody] PunchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name) && string.IsNullOrWhiteSpace(request?.Phone))
                return Json(new { success = false, message = "Please enter your name." });

            var name = request.Name?.Trim();
            var phone = request.Phone?.Trim();
            var hasPhone = !string.IsNullOrWhiteSpace(phone);

            // Find active check-in for today
            var today = NepalToday;
            Attendance? attendance;
            if (hasPhone)
            {
                attendance = await _context.Attendances
                    .Where(a => a.Notes == phone
                             && a.CheckInTime.Date == today
                             && a.CheckOutTime == null)
                    .FirstOrDefaultAsync();
            }
            else
            {
                attendance = await _context.Attendances
                    .Where(a => a.CustomerName.ToLower() == name!.ToLower()
                             && a.CheckInTime.Date == today
                             && a.CheckOutTime == null)
                    .FirstOrDefaultAsync();
            }

            if (attendance == null)
            {
                return Json(new { success = false, message = "No active check-in found. Please punch in first.", notIn = true });
            }

            attendance.CheckOutTime = NepalNow;
            await _context.SaveChangesAsync();

            // Notify iPad display via SignalR
            await _hubContext.Clients.Group("Display").SendAsync("PunchOut", new
            {
                name = attendance.CustomerName,
                time = attendance.CheckOutTime?.ToString("hh:mm tt")
            });

            return Json(new { success = true, message = $"Punched out at {attendance.CheckOutTime:hh:mm tt}. Thank you, see you tomorrow!", type = "out" });
        }

        // GET: /Attendance/CheckStatus - Check if customer is currently in (by phone or name)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CheckStatus(string? phone, string? name)
        {
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(name))
                return Json(new { checkedIn = false });

            var today = NepalToday;
            Attendance? active;

            if (!string.IsNullOrWhiteSpace(phone))
            {
                active = await _context.Attendances
                    .Where(a => a.Notes == phone.Trim()
                             && a.CheckInTime.Date == today
                             && a.CheckOutTime == null)
                    .FirstOrDefaultAsync();
            }
            else
            {
                active = await _context.Attendances
                    .Where(a => a.CustomerName.ToLower() == name!.Trim().ToLower()
                             && a.CheckInTime.Date == today
                             && a.CheckOutTime == null)
                    .FirstOrDefaultAsync();
            }

            return Json(new
            {
                checkedIn = active != null,
                checkInTime = active?.CheckInTime.ToString("hh:mm tt"),
                name = active?.CustomerName
            });
        }

        // POST: /Attendance/ClearAll - Clear all attendance records for a date (admin only)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ClearAll([FromBody] ClearAllRequest request)
        {
            var targetDate = string.IsNullOrEmpty(request?.Date)
                ? NepalToday
                : DateTime.Parse(request.Date).Date;

            var records = await _context.Attendances
                .Where(a => a.CheckInTime.Date == targetDate)
                .ToListAsync();

            if (records.Count == 0)
                return Json(new { success = false, message = "No records found for this date." });

            _context.Attendances.RemoveRange(records);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Cleared {records.Count} records." });
        }

        // GET: /Attendance/History - Attendance history with date range (admin only)
        [Authorize]
        public async Task<IActionResult> History(string? search, DateTime? from, DateTime? to)
        {
            var fromDate = from ?? NepalToday.AddDays(-30);
            var toDate = to ?? NepalToday;

            var query = _context.Attendances
                .Where(a => a.CheckInTime.Date >= fromDate.Date && a.CheckInTime.Date <= toDate.Date)
                .OrderByDescending(a => a.CheckInTime)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.CustomerName.Contains(search));
            }

            var records = await query.ToListAsync();

            ViewBag.Search = search;
            ViewBag.From = fromDate.ToString("yyyy-MM-dd");
            ViewBag.To = toDate.ToString("yyyy-MM-dd");
            ViewBag.TotalRecords = records.Count;
            ViewBag.UniqueCustomers = records.Select(a => a.Notes ?? a.CustomerName).Distinct().Count();

            return View(records);
        }

        // POST: /Attendance/AutoCheckIn - Automatic check-in using device token
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AutoCheckIn([FromBody] DeviceTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
                return Json(new { success = false, message = "No device token." });

            var device = await _context.DeviceTokens
                .FirstOrDefaultAsync(d => d.Token == request.Token && d.IsActive);

            if (device == null)
                return Json(new { success = false, message = "Invalid or expired token.", invalidToken = true });

            // Check if already punched in today
            var today = NepalToday;
            var existing = await _context.Attendances
                .Where(a => a.Notes == device.Phone
                         && a.CheckInTime.Date == today
                         && a.CheckOutTime == null)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return Json(new { success = true, message = $"Already checked in at {existing.CheckInTime:hh:mm tt}.", alreadyIn = true, name = device.CustomerName, checkInTime = existing.CheckInTime.ToString("hh:mm tt") });
            }

            // Find linked customer
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == device.Phone);

            var nepalNow = NepalNow;
            var attendance = new Attendance
            {
                CustomerID = customer?.CustomerID ?? device.CustomerID ?? 0,
                CustomerName = customer?.FullName ?? device.CustomerName,
                CheckInTime = nepalNow,
                CheckOutTime = null,
                Notes = device.Phone
            };

            _context.Attendances.Add(attendance);

            // Update last used
            device.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify iPad display via SignalR
            await _hubContext.Clients.Group("Display").SendAsync("PunchIn", new
            {
                name = attendance.CustomerName,
                time = attendance.CheckInTime.ToString("hh:mm tt")
            });

            return Json(new { success = true, message = $"Punched in at {attendance.CheckInTime:hh:mm tt}. Enjoy your workout!", name = attendance.CustomerName, type = "in" });
        }

        // POST: /Attendance/RegisterDevice - Register device token after first successful punch-in
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name) || string.IsNullOrWhiteSpace(request?.Phone))
                return Json(new { success = false, message = "Name and phone are required." });

            var phone = request.Phone.Trim();
            var name = request.Name.Trim();

            // Generate a secure random token
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToHexString(tokenBytes).ToLower();

            // Check if device already registered with this phone
            var existing = await _context.DeviceTokens
                .FirstOrDefaultAsync(d => d.Phone == phone && d.IsActive);
            if (existing != null)
            {
                // Return existing token
                existing.LastUsedAt = DateTime.UtcNow;
                existing.CustomerName = name;
                await _context.SaveChangesAsync();
                return Json(new { success = true, token = existing.Token });
            }

            // Link to customer if found
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);

            var deviceToken = new DeviceToken
            {
                Token = token,
                CustomerName = customer?.FullName ?? name,
                Phone = phone,
                CustomerID = customer?.CustomerID,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.DeviceTokens.Add(deviceToken);
            await _context.SaveChangesAsync();

            return Json(new { success = true, token = token });
        }

        // POST: /Attendance/ValidateDevice - Check if device token is valid
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateDevice([FromBody] DeviceTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
                return Json(new { valid = false });

            var device = await _context.DeviceTokens
                .FirstOrDefaultAsync(d => d.Token == request.Token && d.IsActive);

            if (device == null)
                return Json(new { valid = false });

            return Json(new { valid = true, name = device.CustomerName, phone = device.Phone });
        }
    }

    public class PunchRequest
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
    }

    public class ClearAllRequest
    {
        public string? Date { get; set; }
    }

    public class DeviceTokenRequest
    {
        public string? Token { get; set; }
    }

    public class RegisterDeviceRequest
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
    }
}
