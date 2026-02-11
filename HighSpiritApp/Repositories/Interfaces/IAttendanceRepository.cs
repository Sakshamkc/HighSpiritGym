using HighSpiritApp.Models.Attendance;

namespace HighSpiritApp.Repositories.Interfaces
{
    /// <summary>
    /// Attendance-specific repository operations
    /// </summary>
    public interface IAttendanceRepository : IRepository<AttendanceRecord>
    {
        Task<AttendanceRecord?> GetTodayRecordAsync(int customerId);
        Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateOnly date);
        Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(DateOnly from, DateOnly to);
        IQueryable<AttendanceRecord> GetQueryable();
    }
}
