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
            membership.CreatedAt = DateTime.Now;
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
            membership.CreatedAt = DateTime.Now;
            await _membershipRepository.AddAsync(membership);

            // Update customer's UpdatedAt so they appear in "Recently Updated"
            var customer = await _customerRepository.GetByIdAsync(membership.CustomerID);
            if (customer != null)
            {
                customer.UpdatedAt = DateTime.Now;
                _customerRepository.Update(customer);
            }

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

            membership.UpdatedAt = DateTime.Now;
            _membershipRepository.Update(membership);
            await _membershipRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int membershipId)
        {
            var membership = await _membershipRepository.GetByIdAsync(membershipId);
            if (membership == null)
                throw new KeyNotFoundException($"Membership with ID {membershipId} not found.");

            _membershipRepository.Remove(membership);
            await _membershipRepository.SaveChangesAsync();
        }

        public async Task HoldAsync(int membershipId)
        {
            var membership = await _membershipRepository.GetByIdAsync(membershipId);
            if (membership == null)
                throw new KeyNotFoundException($"Membership with ID {membershipId} not found.");

            if (membership.IsOnHold)
                throw new InvalidOperationException("Membership is already on hold.");

            if (membership.ExpireDate < DateTime.Today)
                throw new InvalidOperationException("Cannot hold an expired membership.");

            membership.IsOnHold = true;
            membership.HoldStartDate = DateTime.Today;
            membership.UpdatedAt = DateTime.Now;

            _membershipRepository.Update(membership);
            await _membershipRepository.SaveChangesAsync();
        }

        public async Task ResumeAsync(int membershipId)
        {
            var membership = await _membershipRepository.GetByIdAsync(membershipId);
            if (membership == null)
                throw new KeyNotFoundException($"Membership with ID {membershipId} not found.");

            if (!membership.IsOnHold)
                throw new InvalidOperationException("Membership is not on hold.");

            // Calculate how many days it was on hold
            var holdDays = (DateTime.Today - membership.HoldStartDate!.Value).Days;
            if (holdDays < 0) holdDays = 0;

            // Extend expire date by the hold days
            membership.ExpireDate = membership.ExpireDate.AddDays(holdDays);
            membership.TotalHoldDays += holdDays;
            membership.IsOnHold = false;
            membership.HoldStartDate = null;
            membership.UpdatedAt = DateTime.Now;

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

            // Get latest membership by StartDate (no IsActive filter)
            var lastMembership = customer.Memberships?
                .OrderByDescending(m => m.StartDate)
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
