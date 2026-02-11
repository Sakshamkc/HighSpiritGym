using HighSpiritApp.DataContext;
using HighSpiritApp.Models.Attendance;
using HighSpiritApp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Repositories
{
    /// <summary>
    /// Attendance repository implementation
    /// </summary>
    public class AttendanceRepository : Repository<AttendanceRecord>, IAttendanceRepository
    {
        public AttendanceRepository(GymDbContext context) : base(context) { }

        public async Task<AttendanceRecord?> GetTodayRecordAsync(int customerId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return await _dbSet
                .FirstOrDefaultAsync(a => a.CustomerID == customerId && a.Date == today);
        }

        public async Task<IEnumerable<AttendanceRecord>> GetByDateAsync(DateOnly date)
        {
            return await _dbSet
                .Where(a => a.Date == date)
                .OrderByDescending(a => a.PunchInTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(DateOnly from, DateOnly to)
        {
            return await _dbSet
                .Where(a => a.Date >= from && a.Date <= to)
                .OrderByDescending(a => a.PunchInTime)
                .ToListAsync();
        }

        public IQueryable<AttendanceRecord> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }
    }
}
