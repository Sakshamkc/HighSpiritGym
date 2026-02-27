using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HighSpiritApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class AttendanceController : ControllerBase
    {
        private readonly GymDbContext _context;

        public AttendanceController(GymDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET api/attendance/my-qr
        /// Get QR token for the currently authenticated customer
        /// </summary>
        [HttpGet("my-qr")]
        public async Task<IActionResult> GetMyQrToken()
        {
            var customerIdClaim = User.FindFirstValue("CustomerId");
            if (string.IsNullOrEmpty(customerIdClaim))
                return BadRequest(ApiResponse.Fail("No customer profile linked."));

            var customerId = int.Parse(customerIdClaim);
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                return NotFound(ApiResponse.Fail("Customer not found."));

            // Generate QR token if not exists
            if (string.IsNullOrEmpty(customer.QrToken))
            {
                customer.QrToken = Guid.NewGuid().ToString();
                await _context.SaveChangesAsync();
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                QrToken = customer.QrToken,
                CustomerName = customer.FullName
            }));
        }

        /// <summary>
        /// POST api/attendance/checkin
        /// QR-based check-in for customer (accepts customerID or qrToken)
        /// </summary>
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] QrCheckInRequest request)
        {
            Customer? customer = null;

            // Try QR token first (more secure)
            if (!string.IsNullOrEmpty(request.QrToken))
            {
                customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.QrToken == request.QrToken);
                    
                if (customer == null)
                    return NotFound(ApiResponse.Fail("Invalid QR code."));
            }
            // Fall back to customer ID
            else if (request.CustomerID > 0)
            {
                customer = await _context.Customers.FindAsync(request.CustomerID);
                if (customer == null)
                    return NotFound(ApiResponse.Fail("Customer not found."));
            }
            else
            {
                return BadRequest(ApiResponse.Fail("Invalid check-in request."));
            }

            // Check if already checked in today without checkout
            var existingCheckin = await _context.Attendances
                .Where(a => a.CustomerID == customer.CustomerID
                    && a.CheckInTime.Date == DateTime.Today
                    && a.CheckOutTime == null)
                .FirstOrDefaultAsync();

            if (existingCheckin != null)
                return BadRequest(ApiResponse.Fail("Already checked in today. Please check out first."));

            var attendance = new Attendance
            {
                CustomerID = customer.CustomerID,
                CustomerName = customer.FullName,
                CheckInTime = DateTime.Now
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<AttendanceDto>.Ok(MapToDto(attendance), "Checked in successfully."));
        }

        /// <summary>
        /// POST api/attendance/checkout/{customerId}
        /// Check out customer
        /// </summary>
        [HttpPost("checkout/{customerId}")]
        public async Task<IActionResult> CheckOut(int customerId)
        {
            var attendance = await _context.Attendances
                .Where(a => a.CustomerID == customerId && a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefaultAsync();

            if (attendance == null)
                return NotFound(ApiResponse.Fail("No active check-in found."));

            attendance.CheckOutTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<AttendanceDto>.Ok(MapToDto(attendance), "Checked out successfully."));
        }

        /// <summary>
        /// GET api/attendance/today
        /// Get today's attendance list
        /// </summary>
        [HttpGet("today")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            var records = await _context.Attendances
                .Where(a => a.CheckInTime.Date == DateTime.Today)
                .OrderByDescending(a => a.CheckInTime)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records.Select(MapToDto)));
        }

        /// <summary>
        /// GET api/attendance/customer/{customerId}
        /// Get attendance history for a customer
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetCustomerAttendance(int customerId, [FromQuery] int days = 30)
        {
            var fromDate = DateTime.Today.AddDays(-days);
            var records = await _context.Attendances
                .Where(a => a.CustomerID == customerId && a.CheckInTime >= fromDate)
                .OrderByDescending(a => a.CheckInTime)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records.Select(MapToDto)));
        }

        /// <summary>
        /// GET api/attendance/stats
        /// Get attendance statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var todayCount = await _context.Attendances.CountAsync(a => a.CheckInTime.Date == today);
            var monthCount = await _context.Attendances.CountAsync(a => a.CheckInTime >= thisMonth);
            var currentlyIn = await _context.Attendances.CountAsync(a => a.CheckInTime.Date == today && a.CheckOutTime == null);

            return Ok(ApiResponse<object>.Ok(new
            {
                TodayCheckIns = todayCount,
                MonthCheckIns = monthCount,
                CurrentlyInGym = currentlyIn
            }));
        }

        /// <summary>
        /// GET api/attendance/status/{customerId}
        /// Check if customer is currently checked in
        /// </summary>
        [HttpGet("status/{customerId}")]
        public async Task<IActionResult> GetStatus(int customerId)
        {
            var activeCheckin = await _context.Attendances
                .Where(a => a.CustomerID == customerId && a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefaultAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                IsCheckedIn = activeCheckin != null,
                CheckInTime = activeCheckin?.CheckInTime
            }));
        }

        private static AttendanceDto MapToDto(Attendance a) => new()
        {
            AttendanceID = a.AttendanceID,
            CustomerID = a.CustomerID,
            CustomerName = a.CustomerName,
            CheckInTime = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            Notes = a.Notes
        };
    }
}
