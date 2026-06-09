using HighSpiritApp.Models;

namespace HighSpiritApp.Services.Interfaces
{
    public interface IStaffService
    {
        Task<IEnumerable<Staff>> GetAllAsync();
        Task<IEnumerable<Staff>> SearchAsync(string? search);
        Task<IEnumerable<Staff>> GetActiveStaffAsync();
        Task<Staff?> GetByIdAsync(int id);
        Task<Staff> CreateAsync(Staff staff, byte[]? photo);
        Task UpdateAsync(Staff staff, byte[]? photo);
        Task DeleteAsync(int id);
        Task<string> GenerateQrTokenAsync(int staffId);

        // Staff attendance
        Task<StaffAttendance> PunchInAsync(int staffId);
        Task<StaffAttendance?> PunchOutAsync(int staffId);
        Task<StaffAttendance> ManualPunchInAsync(int staffId, DateTime checkInTime);
        Task<StaffAttendance?> ManualPunchOutAsync(int attendanceId, DateTime checkOutTime);
        Task DeleteAttendanceAsync(int attendanceId);
        Task<List<StaffAttendance>> GetAttendanceHistoryAsync(int? staffId, DateTime? from, DateTime? to);
        Task<StaffAttendance?> GetActiveCheckInAsync(int staffId);
    }
}
