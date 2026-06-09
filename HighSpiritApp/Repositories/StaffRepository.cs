using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Repositories
{
    public class StaffRepository : Repository<Staff>, IStaffRepository
    {
        public StaffRepository(GymDbContext context) : base(context) { }

        public async Task<IEnumerable<Staff>> SearchAsync(string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return await _context.Staff.OrderBy(s => s.FullName).ToListAsync();

            return await _context.Staff
                .Where(s => s.FullName.Contains(searchTerm) || s.Phone.Contains(searchTerm))
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Staff>> GetActiveStaffAsync()
        {
            return await _context.Staff
                .Where(s => s.IsActive)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<Staff?> GetByQrTokenAsync(string qrToken)
        {
            return await _context.Staff
                .FirstOrDefaultAsync(s => s.QrToken == qrToken && s.IsActive);
        }
    }
}
