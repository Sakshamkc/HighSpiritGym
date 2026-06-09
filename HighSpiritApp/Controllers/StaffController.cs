using HighSpiritApp.Models;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    [Authorize]
    public class StaffController : Controller
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var staff = await _staffService.SearchAsync(search);
            ViewBag.Search = search;
            ViewBag.TotalStaff = staff.Count();
            ViewBag.ActiveStaff = staff.Count(s => s.IsActive);
            return View(staff.ToList());
        }

        public IActionResult Create()
        {
            return View(new Staff { JoinDate = DateTime.Today });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Staff staff, IFormFile? photoFile)
        {
            byte[]? photo = null;
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                photo = ms.ToArray();
            }

            await _staffService.CreateAsync(staff, photo);
            TempData["success"] = $"Staff member {staff.FullName} added successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Staff staff, IFormFile? photoFile)
        {
            byte[]? photo = null;
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                photo = ms.ToArray();
            }

            await _staffService.UpdateAsync(staff, photo);
            TempData["success"] = $"Staff member {staff.FullName} updated successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff == null) return NotFound();

            var activeCheckIn = await _staffService.GetActiveCheckInAsync(id);
            var history = await _staffService.GetAttendanceHistoryAsync(id, null, null);

            ViewBag.ActiveCheckIn = activeCheckIn;
            ViewBag.AttendanceHistory = history;
            return View(staff);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _staffService.DeleteAsync(id);
            TempData["success"] = "Staff member deleted successfully!";
            return RedirectToAction("Index");
        }

        // GET: Staff photo
        [AllowAnonymous]
        public async Task<IActionResult> Photo(int id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff?.Photo == null || staff.Photo.Length == 0)
                return NotFound();
            return File(staff.Photo, "image/jpeg");
        }

        // POST: Generate/Regenerate QR token
        [HttpPost]
        public async Task<IActionResult> RegenerateQr(int id)
        {
            await _staffService.GenerateQrTokenAsync(id);
            TempData["success"] = "QR code regenerated!";
            return RedirectToAction("Details", new { id });
        }

        // GET: Download QR code as image
        [AllowAnonymous]
        public async Task<IActionResult> DownloadQr(int id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff == null || string.IsNullOrEmpty(staff.QrToken))
                return NotFound();

            // Return QR data - frontend will render using a JS library
            return Json(new { token = staff.QrToken, name = staff.FullName });
        }

        // =========================================================
        // STAFF ATTENDANCE
        // =========================================================

        public async Task<IActionResult> Attendance(int? staffId, DateTime? from, DateTime? to)
        {
            var staffList = (await _staffService.GetActiveStaffAsync()).ToList();
            var history = await _staffService.GetAttendanceHistoryAsync(staffId, from, to);

            ViewBag.StaffList = staffList;
            ViewBag.SelectedStaffId = staffId;
            ViewBag.From = (from ?? DateTime.Today.AddDays(-30)).ToString("yyyy-MM-dd");
            ViewBag.To = (to ?? DateTime.Today).ToString("yyyy-MM-dd");
            ViewBag.TotalRecords = history.Count;

            return View(history);
        }

        [HttpPost]
        public async Task<IActionResult> PunchIn(int staffId, string? returnTo)
        {
            try
            {
                await _staffService.PunchInAsync(staffId);
                TempData["success"] = "Staff punched in successfully!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
            }
            if (returnTo == "attendance")
                return RedirectToAction("Attendance");
            return RedirectToAction("Details", new { id = staffId });
        }

        [HttpPost]
        public async Task<IActionResult> PunchOut(int staffId, string? returnTo)
        {
            var result = await _staffService.PunchOutAsync(staffId);
            if (result == null)
                TempData["error"] = "No active check-in found.";
            else
                TempData["success"] = "Staff punched out successfully!";
            if (returnTo == "attendance")
                return RedirectToAction("Attendance");
            return RedirectToAction("Details", new { id = staffId });
        }

        [HttpPost]
        public async Task<IActionResult> ManualPunchIn(int staffId, DateTime checkInTime)
        {
            try
            {
                await _staffService.ManualPunchInAsync(staffId, checkInTime);
                TempData["success"] = "Manual punch in recorded!";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return RedirectToAction("Attendance");
        }

        [HttpPost]
        public async Task<IActionResult> ManualPunchOut(int attendanceId, DateTime checkOutTime)
        {
            var result = await _staffService.ManualPunchOutAsync(attendanceId, checkOutTime);
            if (result == null)
                TempData["error"] = "Attendance record not found.";
            else
                TempData["success"] = "Manual punch out recorded!";
            return RedirectToAction("Attendance");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttendance(int attendanceId)
        {
            await _staffService.DeleteAttendanceAsync(attendanceId);
            TempData["success"] = "Attendance record deleted.";
            return RedirectToAction("Attendance");
        }

        // GET: /Staff/QrScan - Reception page for USB QR scanner
        [Authorize]
        public IActionResult QrScan()
        {
            return View();
        }

        // GET: /Staff/MobileScan - Mobile camera-based QR scanner for gym owner
        [Authorize]
        public IActionResult MobileScan()
        {
            return View();
        }

        // QR-based staff check-in (requires login)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> QrCheckIn([FromBody] StaffQrRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.QrToken))
                return Json(new { success = false, message = "Invalid QR code." });

            var staff = (await _staffService.GetAllAsync())
                .FirstOrDefault(s => s.QrToken == request.QrToken && s.IsActive);

            if (staff == null)
                return Json(new { success = false, message = "Staff not found or inactive." });

            try
            {
                var attendance = await _staffService.PunchInAsync(staff.StaffID);
                return Json(new { success = true, message = $"{staff.FullName} punched in at {attendance.CheckInTime:hh:mm tt}", name = staff.FullName });
            }
            catch (InvalidOperationException)
            {
                // Already checked in - try punch out
                var result = await _staffService.PunchOutAsync(staff.StaffID);
                if (result != null)
                    return Json(new { success = true, message = $"{staff.FullName} punched out at {result.CheckOutTime:hh:mm tt}", name = staff.FullName, type = "out" });
                return Json(new { success = false, message = "Already checked in today." });
            }
        }
    }

    public class StaffQrRequest
    {
        public string? QrToken { get; set; }
    }
}
