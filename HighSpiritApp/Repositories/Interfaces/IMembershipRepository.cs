using HighSpiritApp.Models;

namespace HighSpiritApp.Repositories.Interfaces
{
    /// <summary>
    /// Membership-specific repository operations
    /// </summary>
    public interface IMembershipRepository : IRepository<CustomerMembership>
    {
        Task<CustomerMembership?> GetLatestByCustomerIdAsync(int customerId);
        Task<IEnumerable<CustomerMembership>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<CustomerMembership>> GetExpiredMembershipsAsync();
        Task<IEnumerable<CustomerMembership>> GetExpiringSoonAsync(int days = 7);
        Task<CustomerMembership?> GetActiveByCustomerIdAsync(int customerId);
        Task DeactivateByCustomerIdAsync(int customerId);
        Task<IEnumerable<CustomerMembership>> GetAllWithCustomerAsync();
    }
}
