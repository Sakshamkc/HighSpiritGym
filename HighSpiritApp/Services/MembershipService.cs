using HighSpiritApp.Models;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;

namespace HighSpiritApp.Services
{
    /// <summary>
    /// Membership business logic implementation
    /// </summary>
    public class MembershipService : IMembershipService
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly ICustomerRepository _customerRepository;

        public MembershipService(
            IMembershipRepository membershipRepository,
            ICustomerRepository customerRepository)
        {
            _membershipRepository = membershipRepository;
            _customerRepository = customerRepository;
        }

        public async Task<CustomerMembership?> GetByIdAsync(int id)
        {
            return await _membershipRepository.GetByIdAsync(id);
        }

        public async Task<CustomerMembership?> GetLatestByCustomerIdAsync(int customerId)
        {
            return await _membershipRepository.GetLatestByCustomerIdAsync(customerId);
        }

        public async Task<IEnumerable<CustomerMembership>> GetByCustomerIdAsync(int customerId)
        {
            return await _membershipRepository.GetByCustomerIdAsync(customerId);
        }

        public async Task<IEnumerable<CustomerMembership>> GetExpiredMembershipsAsync()
        {
            return await _membershipRepository.GetExpiredMembershipsAsync();
        }

        public async Task<IEnumerable<CustomerMembership>> GetExpiringSoonAsync(int days = 7)
        {
            return await _membershipRepository.GetExpiringSoonAsync(days);
        }

        public async Task CreateAsync(CustomerMembership membership)
        {
            // Calculate expire date if not set
            if (membership.ExpireDate == default)
            {
                membership.ExpireDate = membership.StartDate.AddMonths(membership.Duration);
            }

            membership.IsActive = true;
            await _membershipRepository.AddAsync(membership);
            await _membershipRepository.SaveChangesAsync();
        }

        public async Task RenewAsync(CustomerMembership membership)
        {
            // Calculate expire date if not set
            if (membership.ExpireDate == default || membership.ExpireDate == DateTime.MinValue)
            {
                membership.ExpireDate = membership.StartDate.AddMonths(membership.Duration);
            }

            // Validate dates
            if (membership.ExpireDate < membership.StartDate)
            {
                throw new ArgumentException("Expire date cannot be earlier than start date.");
            }

            // Deactivate previous active membership
            await _membershipRepository.DeactivateByCustomerIdAsync(membership.CustomerID);

            membership.IsActive = true;
            await _membershipRepository.AddAsync(membership);
            await _membershipRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(int membershipId, string? planName, int? paidPrice, DateTime? startDate, DateTime? expireDate)
        {
            var membership = await _membershipRepository.GetByIdAsync(membershipId);
            if (membership == null)
                throw new KeyNotFoundException($"Membership with ID {membershipId} not found.");

            if (planName != null) membership.PlanName = planName;
            if (paidPrice.HasValue) membership.PaidPrice = paidPrice.Value;
            if (startDate.HasValue) membership.StartDate = startDate.Value;
            if (expireDate.HasValue) membership.ExpireDate = expireDate.Value;

            _membershipRepository.Update(membership);
            await _membershipRepository.SaveChangesAsync();
        }

        public DateTime CalculateSuggestedStartDate(int customerId)
        {
            // This is sync for simplicity, but you could make it async
            var lastMembership = _membershipRepository
                .GetActiveByCustomerIdAsync(customerId).Result;

            if (lastMembership != null && lastMembership.ExpireDate >= DateTime.Today)
            {
                return lastMembership.ExpireDate.AddDays(1);
            }

            return DateTime.Today;
        }

        public async Task<RenewalInfo> GetRenewalInfoAsync(int customerId)
        {
            var customer = await _customerRepository.GetByIdWithMembershipsAsync(customerId);
            if (customer == null)
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");

            var lastMembership = customer.Memberships?
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.ExpireDate)
                .FirstOrDefault();

            DateTime suggestedStart;
            if (lastMembership != null && lastMembership.ExpireDate >= DateTime.Today)
            {
                suggestedStart = lastMembership.ExpireDate.AddDays(1);
            }
            else
            {
                suggestedStart = DateTime.Today;
            }

            return new RenewalInfo
            {
                CustomerId = customerId,
                CustomerName = customer.FullName,
                LastExpireDate = lastMembership?.ExpireDate,
                SuggestedStartDate = suggestedStart,
                LastPlanName = lastMembership?.PlanName
            };
        }
    }
}
