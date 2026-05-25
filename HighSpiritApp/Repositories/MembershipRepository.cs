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

            // Project memberships and customer fields WITHOUT loading the heavy Photo byte[].
            // Loading the full Customer (with Photo blob) for every membership was causing
            // huge DB transfers and slow page loads on every notification render.
            var memberships = await _context.CustomerMemberships
                .AsNoTracking()
                .Select(m => new CustomerMembership
                {
                    MembershipID = m.MembershipID,
                    CustomerID = m.CustomerID,
                    PlanName = m.PlanName,
                    StartDate = m.StartDate,
                    ExpireDate = m.ExpireDate,
                    Duration = m.Duration,
                    PaidPrice = m.PaidPrice,
                    DueAmount = m.DueAmount,
                    IsActive = m.IsActive,
                    IsOnHold = m.IsOnHold,
                    HoldStartDate = m.HoldStartDate,
                    TotalHoldDays = m.TotalHoldDays,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    Customer = new Customer
                    {
                        CustomerID = m.Customer!.CustomerID,
                        FullName = m.Customer.FullName,
                        Phone = m.Customer.Phone,
                        Gender = m.Customer.Gender,
                        Shift = m.Customer.Shift,
                        JoinDate = m.Customer.JoinDate
                    }
                })
                .ToListAsync();

            // Group by customer and get the latest by StartDate, then filter expired
            return memberships
                .GroupBy(m => m.CustomerID)
                .Select(g => g.OrderByDescending(x => x.StartDate).First())
                .Where(m => m.ExpireDate < today && !m.IsOnHold)
                .OrderByDescending(m => m.ExpireDate)
                .ToList();
        }

        public async Task<IEnumerable<CustomerMembership>> GetExpiringSoonAsync(int days = 7)
        {
            var today = DateTime.Today;
            var endDate = today.AddDays(days);

            // Project without the heavy Photo blob (see GetExpiredMembershipsAsync above).
            var memberships = await _context.CustomerMemberships
                .AsNoTracking()
                .Select(m => new CustomerMembership
                {
                    MembershipID = m.MembershipID,
                    CustomerID = m.CustomerID,
                    PlanName = m.PlanName,
                    StartDate = m.StartDate,
                    ExpireDate = m.ExpireDate,
                    Duration = m.Duration,
                    PaidPrice = m.PaidPrice,
                    DueAmount = m.DueAmount,
                    IsActive = m.IsActive,
                    IsOnHold = m.IsOnHold,
                    HoldStartDate = m.HoldStartDate,
                    TotalHoldDays = m.TotalHoldDays,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    Customer = new Customer
                    {
                        CustomerID = m.Customer!.CustomerID,
                        FullName = m.Customer.FullName,
                        Phone = m.Customer.Phone,
                        Gender = m.Customer.Gender,
                        Shift = m.Customer.Shift,
                        JoinDate = m.Customer.JoinDate
                    }
                })
                .ToListAsync();

            // Group by customer and get the latest by StartDate, then filter expiring soon
            return memberships
                .GroupBy(m => m.CustomerID)
                .Select(g => g.OrderByDescending(x => x.StartDate).First())
                .Where(m => m.ExpireDate >= today && m.ExpireDate <= endDate && !m.IsOnHold)
                .OrderBy(m => m.ExpireDate)
                .ToList();
        }

        public async Task<CustomerMembership?> GetActiveByCustomerIdAsync(int customerId)
        {
            // Get latest membership by StartDate, no IsActive filter
            return await _context.CustomerMemberships
                .Where(m => m.CustomerID == customerId)
                .OrderByDescending(m => m.StartDate)
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

        public async Task<IEnumerable<CustomerMembership>> GetAllWithCustomerAsync()
        {
            // Project without heavy Photo blob
            return await _context.CustomerMemberships
                .AsNoTracking()
                .OrderByDescending(m => m.StartDate)
                .Select(m => new CustomerMembership
                {
                    MembershipID = m.MembershipID,
                    CustomerID = m.CustomerID,
                    PlanName = m.PlanName,
                    StartDate = m.StartDate,
                    ExpireDate = m.ExpireDate,
                    Duration = m.Duration,
                    PaidPrice = m.PaidPrice,
                    DueAmount = m.DueAmount,
                    IsActive = m.IsActive,
                    IsOnHold = m.IsOnHold,
                    HoldStartDate = m.HoldStartDate,
                    TotalHoldDays = m.TotalHoldDays,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    Customer = new Customer
                    {
                        CustomerID = m.Customer!.CustomerID,
                        FullName = m.Customer.FullName,
                        Phone = m.Customer.Phone,
                        Gender = m.Customer.Gender,
                        Shift = m.Customer.Shift,
                        JoinDate = m.Customer.JoinDate
                    }
                })
                .ToListAsync();
        }
    }
}
