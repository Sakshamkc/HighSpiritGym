using HighSpiritApp.Models;

namespace HighSpiritApp.Services.Interfaces
{
    /// <summary>
    /// Membership business logic service
    /// </summary>
    public interface IMembershipService
    {
        Task<CustomerMembership?> GetByIdAsync(int id);
        Task<CustomerMembership?> GetLatestByCustomerIdAsync(int customerId);
        Task<IEnumerable<CustomerMembership>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<CustomerMembership>> GetExpiredMembershipsAsync();
        Task<IEnumerable<CustomerMembership>> GetExpiringSoonAsync(int days = 7);
        Task CreateAsync(CustomerMembership membership);
        Task RenewAsync(CustomerMembership membership);
        Task UpdateAsync(int membershipId, string? planName, int? paidPrice, DateTime? startDate, DateTime? expireDate);
        DateTime CalculateSuggestedStartDate(int customerId);
        Task<RenewalInfo> GetRenewalInfoAsync(int customerId);
    }

    public class RenewalInfo
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime? LastExpireDate { get; set; }
        public DateTime SuggestedStartDate { get; set; }
        public string? LastPlanName { get; set; }
    }
}
