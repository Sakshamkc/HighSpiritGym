using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HighSpiritApp.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staffRepository;
        private readonly GymDbContext _db;

        // Nepal is UTC+5:45
        private static readonly TimeSpan NepalOffset = new TimeSpan(5, 45, 0);
        private static DateTime NepalNow => DateTime.UtcNow.Add(NepalOffset);
        private static DateTime NepalToday => NepalNow.Date;

        public StaffService(IStaffRepository staffRepository, GymDbContext db)
        {
            _staffRepository = staffRepository;
            _db = db;
        }

        public async Task<IEnumerable<Staff>> GetAllAsync()
        {
            return await _staffRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Staff>> SearchAsync(string? search)
        {
            return await _staffRepository.SearchAsync(search);
        }

        public async Task<IEnumerable<Staff>> GetActiveStaffAsync()
        {
            return await _staffRepository.GetActiveStaffAsync();
        }

        public async Task<Staff?> GetByIdAsync(int id)
        {
            return await _staffRepository.GetByIdAsync(id);
        }

        public async Task<Staff> CreateAsync(Staff staff, byte[]? photo)
        {
            if (photo != null)
                staff.Photo = photo;

            staff.CreatedAt = DateTime.Now;
            staff.QrToken = GenerateToken();

            await _staffRepository.AddAsync(staff);
            await _staffRepository.SaveChangesAsync();
            return staff;
        }

        public async Task UpdateAsync(Staff staff, byte[]? photo)
        {
            var existing = await _staffRepository.GetByIdAsync(staff.StaffID);
            if (existing == null)
                throw new KeyNotFoundException($"Staff with ID {staff.StaffID} not found.");

            existing.FullName = staff.FullName;
            existing.Phone = staff.Phone;
            existing.Gender = staff.Gender;
            existing.Age = staff.Age;
            existing.Position = staff.Position;
            existing.IsActive = staff.IsActive;
            existing.JoinDate = staff.JoinDate;
            existing.UpdatedAt = DateTime.Now;

            if (photo != null)
                existing.Photo = photo;

            _staffRepository.Update(existing);
            await _staffRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var staff = await _staffRepository.GetByIdAsync(id);
            if (staff != null)
            {
                _staffRepository.Remove(staff);
                await _staffRepository.SaveChangesAsync();
            }
        }

        public async Task<string> GenerateQrTokenAsync(int staffId)
        {
            var staff = await _staffRepository.GetByIdAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff with ID {staffId} not found.");

            staff.QrToken = GenerateToken();
            staff.UpdatedAt = DateTime.Now;
            _staffRepository.Update(staff);
            await _staffRepository.SaveChangesAsync();
            return staff.QrToken;
        }

        public async Task<StaffAttendance> PunchInAsync(int staffId)
        {
            var staff = await _staffRepository.GetByIdAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException("Staff not found.");

            // Check if already checked in today (still active)
            var activeCheckin = await _db.StaffAttendances
                .Where(a => a.StaffID == staffId && a.CheckInTime.Date == NepalToday && a.CheckOutTime == null)
                .FirstOrDefaultAsync();

            if (activeCheckin != null)
                throw new InvalidOperationException("Already checked in today.");

            // Check if already punched out today (completed shift)
            var completedToday = await _db.StaffAttendances
                .Where(a => a.StaffID == staffId && a.CheckInTime.Date == NepalToday && a.CheckOutTime != null)
                .AnyAsync();

            if (completedToday)
                throw new InvalidOperationException("Already completed shift today. Cannot punch in again.");

            var attendance = new StaffAttendance
            {
                StaffID = staffId,
                StaffName = staff.FullName,
                CheckInTime = NepalNow
            };

            _db.StaffAttendances.Add(attendance);
            await _db.SaveChangesAsync();
            return attendance;
        }

        public async Task<StaffAttendance?> PunchOutAsync(int staffId)
        {
            var attendance = await _db.StaffAttendances
                .Where(a => a.StaffID == staffId && a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefaultAsync();

            if (attendance == null)
                return null;

            attendance.CheckOutTime = NepalNow;
            await _db.SaveChangesAsync();
            return attendance;
        }

        public async Task<List<StaffAttendance>> GetAttendanceHistoryAsync(int? staffId, DateTime? from, DateTime? to)
        {
            var fromDate = from ?? NepalToday.AddDays(-30);
            var toDate = to ?? NepalToday;

            var query = _db.StaffAttendances.AsNoTracking()
                .Where(a => a.CheckInTime.Date >= fromDate.Date && a.CheckInTime.Date <= toDate.Date);

            if (staffId.HasValue)
                query = query.Where(a => a.StaffID == staffId.Value);

            return await query.OrderByDescending(a => a.CheckInTime).ToListAsync();
        }

        public async Task<StaffAttendance?> GetActiveCheckInAsync(int staffId)
        {
            return await _db.StaffAttendances
                .Where(a => a.StaffID == staffId && a.CheckInTime.Date == NepalToday && a.CheckOutTime == null)
                .FirstOrDefaultAsync();
        }

        public async Task<StaffAttendance> ManualPunchInAsync(int staffId, DateTime checkInTime)
        {
            var staff = await _staffRepository.GetByIdAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException("Staff not found.");

            // Prevent punch in if already has a record on that date
            var existsOnDate = await _db.StaffAttendances
                .Where(a => a.StaffID == staffId && a.CheckInTime.Date == checkInTime.Date)
                .AnyAsync();

            if (existsOnDate)
                throw new InvalidOperationException("Staff already has attendance record for this date. Cannot punch in again.");

            var attendance = new StaffAttendance
            {
                StaffID = staffId,
                StaffName = staff.FullName,
                CheckInTime = checkInTime
            };

            _db.StaffAttendances.Add(attendance);
            await _db.SaveChangesAsync();
            return attendance;
        }

        public async Task<StaffAttendance?> ManualPunchOutAsync(int attendanceId, DateTime checkOutTime)
        {
            var attendance = await _db.StaffAttendances.FindAsync(attendanceId);
            if (attendance == null)
                return null;

            if (checkOutTime <= attendance.CheckInTime)
                throw new InvalidOperationException($"Punch out time must be after check-in time ({attendance.CheckInTime:hh:mm tt}).");

            attendance.CheckOutTime = checkOutTime;
            await _db.SaveChangesAsync();
            return attendance;
        }

        public async Task DeleteAttendanceAsync(int attendanceId)
        {
            var attendance = await _db.StaffAttendances.FindAsync(attendanceId);
            if (attendance != null)
            {
                _db.StaffAttendances.Remove(attendance);
                await _db.SaveChangesAsync();
            }
        }

        private static string GenerateToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToHexString(tokenBytes).ToLower();
        }
    }
}
