using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Repositories
{
    /// <summary>
    /// Customer repository implementation
    /// </summary>
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(GymDbContext context) : base(context) { }

        public async Task<Customer?> GetByIdWithMembershipsAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.Memberships)
                .FirstOrDefaultAsync(c => c.CustomerID == id);
        }

        public async Task<IEnumerable<Customer>> GetAllWithMembershipsAsync()
        {
            return await _context.Customers
                .Include(c => c.Memberships)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> SearchAsync(string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return await GetAllWithMembershipsAsync();

            return await _context.Customers
                .Include(c => c.Memberships)
                .Where(c => c.FullName.Contains(searchTerm) ||
                           (c.Phone != null && c.Phone.Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string fullName, DateTime joinDate)
        {
            return await _context.Customers
                .AnyAsync(c => c.FullName == fullName && c.JoinDate.Date == joinDate.Date);
        }

        public IQueryable<Customer> GetQueryable()
        {
            return _context.Customers.Include(c => c.Memberships).AsNoTracking().AsQueryable();
        }
    }
}
