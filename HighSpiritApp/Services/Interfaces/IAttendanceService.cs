using HighSpiritApp.Models.Attendance;

namespace HighSpiritApp.Services.Interfaces
{
    /// <summary>
    /// Attendance service interface for QR-based punch in/out
    /// </summary>
    public interface IAttendanceService
    {
        Task<PunchResult> PunchAsync(int customerId);
        Task<IEnumerable<AttendanceRecord>> GetAttendanceByDateAsync(DateOnly date);
        Task<IEnumerable<AttendanceRecord>> GetAttendanceByRangeAsync(DateOnly from, DateOnly to);
        Task<AttendanceSummary> GetTodaySummaryAsync();
    }

    /// <summary>
    /// Result of a punch in/out operation
    /// </summary>
    public class PunchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PunchType { get; set; } = string.Empty; // "PunchIn", "PunchOut", "Error"
        public string? CustomerName { get; set; }
        public DateTime? Time { get; set; }
    }

    /// <summary>
    /// Attendance summary for dashboard/admin view
    /// </summary>
    public class AttendanceSummary
    {
        public int TotalCheckIns { get; set; }
        public int CurrentlyInGym { get; set; }
        public int AlreadyLeft { get; set; }
    }
}
