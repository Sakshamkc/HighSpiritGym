using HighSpiritApp.DataContext;
using HighSpiritApp.Models.Locker;
using HighSpiritApp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Repositories
{
    /// <summary>
    /// Locker repository implementation
    /// </summary>
    public class LockerRepository : Repository<Locker>, ILockerRepository
    {
        public LockerRepository(GymDbContext context) : base(context) { }

        public async Task<IEnumerable<Locker>> GetAvailableLockersAsync()
        {
            return await _context.Lockers
                .Where(l => l.Status == "Available")
                .OrderBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> GetOccupiedLockersAsync()
        {
            return await _context.Lockers
                .Where(l => l.Status == "Occupied")
                .OrderBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> GetExpiredLockersAsync()
        {
            var today = DateTime.Today;
            return await _context.Lockers
                .Where(l => l.Status == "Occupied" && l.EndDate < today)
                .OrderBy(l => l.EndDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> GetExpiringSoonLockersAsync(int days = 7)
        {
            var today = DateTime.Today;
            var futureDate = today.AddDays(days);
            return await _context.Lockers
                .Where(l => l.Status == "Occupied" && l.EndDate >= today && l.EndDate <= futureDate)
                .OrderBy(l => l.EndDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> GetLockersByStatusAsync(string status)
        {
            return await _context.Lockers
                .Where(l => l.Status == status)
                .OrderBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> GetLockersBySizeAsync(string size)
        {
            return await _context.Lockers
                .Where(l => l.Size == size)
                .OrderBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> SearchAsync(string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return await GetAllAsync();

            return await _context.Lockers
                .Where(l => l.LockerNumber.Contains(searchTerm) ||
                           (l.AssignedTo != null && l.AssignedTo.Contains(searchTerm)) ||
                           (l.AssignedPhone != null && l.AssignedPhone.Contains(searchTerm)) ||
                           (l.KeyNumber != null && l.KeyNumber.Contains(searchTerm)))
                .OrderBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<Locker?> GetByLockerNumberAsync(string lockerNumber)
        {
            return await _context.Lockers
                .FirstOrDefaultAsync(l => l.LockerNumber == lockerNumber);
        }

        public async Task<bool> IsLockerNumberExistsAsync(string lockerNumber, int? excludeId = null)
        {
            return await _context.Lockers
                .AnyAsync(l => l.LockerNumber == lockerNumber && (!excludeId.HasValue || l.LockerID != excludeId.Value));
        }

        public async Task<decimal> GetTotalDueAmountAsync()
        {
            return await _context.Lockers.SumAsync(l => l.DueAmount);
        }
    }
}
