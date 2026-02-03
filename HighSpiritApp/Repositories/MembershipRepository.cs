using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Repositories
{
    /// <summary>
    /// Membership repository implementation
    /// </summary>
    public class MembershipRepository : Repository<CustomerMembership>, IMembershipRepository
    {
        public MembershipRepository(GymDbContext context) : base(context) { }

        public async Task<CustomerMembership?> GetLatestByCustomerIdAsync(int customerId)
        {
            return await _context.CustomerMemberships
                .Include(m => m.Customer)
                .Where(m => m.CustomerID == customerId)
                .OrderByDescending(m => m.StartDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CustomerMembership>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.CustomerMemberships
                .Where(m => m.CustomerID == customerId)
                .OrderByDescending(m => m.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerMembership>> GetExpiredMembershipsAsync()
        {
            var today = DateTime.Today;

            var memberships = await _context.CustomerMemberships
                .Include(m => m.Customer)
                .Where(m => m.ExpireDate != null && m.IsActive)
                .ToListAsync();

            return memberships
                .GroupBy(m => m.CustomerID)
                .Select(g => g.OrderByDescending(x => x.StartDate).First())
                .Where(m => m.ExpireDate < today)
                .OrderByDescending(m => m.ExpireDate)
                .ToList();
        }

        public async Task<IEnumerable<CustomerMembership>> GetExpiringSoonAsync(int days = 7)
        {
            var today = DateTime.Today;
            var endDate = today.AddDays(days);

            var memberships = await _context.CustomerMemberships
                .Include(m => m.Customer)
                .Where(m => m.IsActive)
                .ToListAsync();

            return memberships
                .GroupBy(m => m.CustomerID)
                .Select(g => g.OrderByDescending(x => x.StartDate).First())
                .Where(m => m.ExpireDate >= today && m.ExpireDate <= endDate)
                .OrderBy(m => m.ExpireDate)
                .ToList();
        }

        public async Task<CustomerMembership?> GetActiveByCustomerIdAsync(int customerId)
        {
            return await _context.CustomerMemberships
                .Where(m => m.CustomerID == customerId && m.IsActive)
                .OrderByDescending(m => m.ExpireDate)
                .FirstOrDefaultAsync();
        }

        public async Task DeactivateByCustomerIdAsync(int customerId)
        {
            var activeMemberships = await _context.CustomerMemberships
                .Where(m => m.CustomerID == customerId && m.IsActive)
                .ToListAsync();

            foreach (var membership in activeMemberships)
            {
                membership.IsActive = false;
            }
        }
    }
}
