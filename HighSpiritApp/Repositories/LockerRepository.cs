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
                .Where(l => l.Status == "Available" || l.Status == "Empty")
                .OrderBy(l => l.Gender)
                .ThenBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> GetOccupiedLockersAsync()
        {
            return await _context.Lockers
                .Where(l => l.Status == "Occupied")
                .OrderBy(l => l.Gender)
                .ThenBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> GetExpiredLockersAsync()
        {
            var today = DateTime.Today;
            return await _context.Lockers
                .Where(l => l.Status == "Occupied" && l.EndDate < today)
                .OrderBy(l => l.LockerNumber)
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
                .OrderBy(l => l.Gender)
                .ThenBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> GetLockersByGenderAsync(string gender)
        {
            return await _context.Lockers
                .Where(l => l.Gender == gender)
                .OrderBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Locker>> SearchAsync(string? searchTerm, string? gender = null)
        {
            var query = _context.Lockers.AsQueryable();

            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(l => l.Gender == gender);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(l => 
                    l.LockerNumber.Contains(searchTerm) ||
                    (l.AssignedTo != null && l.AssignedTo.Contains(searchTerm)) ||
                    (l.AssignedPhone != null && l.AssignedPhone.Contains(searchTerm)) ||
                    (l.Package != null && l.Package.Contains(searchTerm)));
            }

            return await query
                .OrderBy(l => l.LockerNumber)
                .ToListAsync();
        }

        public async Task<Locker?> GetByLockerNumberAsync(string lockerNumber, string gender)
        {
            return await _context.Lockers
                .FirstOrDefaultAsync(l => l.LockerNumber == lockerNumber && l.Gender == gender);
        }

        public async Task<bool> IsLockerNumberExistsAsync(string lockerNumber, string gender, int? excludeId = null)
        {
            return await _context.Lockers
                .AnyAsync(l => l.LockerNumber == lockerNumber && 
                              l.Gender == gender && 
                              (!excludeId.HasValue || l.LockerID != excludeId.Value));
        }

        public async Task<decimal> GetTotalDueAmountAsync()
        {
            return await _context.Lockers.SumAsync(l => l.DueAmount);
        }

        public async Task<int> GetCountByGenderAsync(string gender)
        {
            return await _context.Lockers.CountAsync(l => l.Gender == gender);
        }
    }
}
