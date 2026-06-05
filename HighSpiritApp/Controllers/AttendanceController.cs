using HighSpiritApp.DataContext;
using HighSpiritApp.Hubs;
using HighSpiritApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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

            // Check if already punched in today (not yet punched out)
            var today = NepalToday;
            var existing = await _context.Attendances
                .Where(a => a.CustomerName.ToLower() == name.ToLower()
                         && a.CheckInTime.Date == today
                         && a.CheckOutTime == null)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return Json(new { success = false, message = "You are already checked in. Please punch out first.", alreadyIn = true });
            }

            // Find customer by name (optional - for linking)
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.FullName.ToLower() == name.ToLower());

            var nepalNow = NepalNow;
            var attendance = new Attendance
            {
                CustomerID = customer?.CustomerID ?? 0,
                CustomerName = customer?.FullName ?? name,
                CheckInTime = nepalNow,
                CheckOutTime = null
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
            if (string.IsNullOrWhiteSpace(request?.Name))
                return Json(new { success = false, message = "Please enter your name." });

            var name = request.Name.Trim();

            // Find active check-in for today
            var today = NepalToday;
            var attendance = await _context.Attendances
                .Where(a => a.CustomerName.ToLower() == name.ToLower()
                         && a.CheckInTime.Date == today
                         && a.CheckOutTime == null)
                .FirstOrDefaultAsync();

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

        // GET: /Attendance/CheckStatus?name=xxx - Check if customer is currently in
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CheckStatus(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { checkedIn = false });

            var today = NepalToday;
            var active = await _context.Attendances
                .Where(a => a.CustomerName.ToLower() == name.Trim().ToLower()
                         && a.CheckInTime.Date == today
                         && a.CheckOutTime == null)
                .FirstOrDefaultAsync();

            return Json(new
            {
                checkedIn = active != null,
                checkInTime = active?.CheckInTime.ToString("hh:mm tt")
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
    }

    public class PunchRequest
    {
        public string? Name { get; set; }
    }

    public class ClearAllRequest
    {
        public string? Date { get; set; }
    }
}
