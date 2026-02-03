using HighSpiritApp.DataContext;
using HighSpiritApp.Models.Boxing;
using HighSpiritApp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Repositories
{
    /// <summary>
    /// Boxing member repository implementation
    /// </summary>
    public class BoxingRepository : Repository<BoxingMember>, IBoxingRepository
    {
        public BoxingRepository(GymDbContext context) : base(context) { }

        public async Task<IEnumerable<BoxingMember>> GetMembersWithDueAsync()
        {
            return await _context.BoxingMembers
                .Where(b => b.DueAmount > 0)
                .OrderByDescending(b => b.DueAmount)
                .ToListAsync();
        }

        public async Task<IEnumerable<BoxingMember>> GetFullyPaidMembersAsync()
        {
            return await _context.BoxingMembers
                .Where(b => b.DueAmount == 0)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalDueAmountAsync()
        {
            return await _context.BoxingMembers.SumAsync(b => b.DueAmount);
        }

        public async Task<IEnumerable<BoxingMember>> SearchAsync(string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return await GetAllAsync();

            return await _context.BoxingMembers
                .Where(b => b.Name.Contains(searchTerm) ||
                           (b.GuardianContact != null && b.GuardianContact.Contains(searchTerm)))
                .ToListAsync();
        }
    }
}
