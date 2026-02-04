using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;

namespace HighSpiritApp.Services
{
    /// <summary>
    /// Dashboard statistics service implementation
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IMembershipRepository _membershipRepository;
        private readonly IBoxingRepository _boxingRepository;

        public DashboardService(
            ICustomerRepository customerRepository,
            IMembershipRepository membershipRepository,
            IBoxingRepository boxingRepository)
        {
            _customerRepository = customerRepository;
            _membershipRepository = membershipRepository;
            _boxingRepository = boxingRepository;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // Get all customers with memberships
            var customers = (await _customerRepository.GetAllWithMembershipsAsync()).ToList();

            // Get latest membership for each customer (by StartDate, no IsActive filter)
            var customerMemberships = customers
                .Select(c => new
                {
                    Customer = c,
                    LatestMembership = c.Memberships?
                        .OrderByDescending(m => m.StartDate)
                        .FirstOrDefault()
                })
                .Where(x => x.LatestMembership != null)
                .ToList();

            // Gym stats - based on latest membership's ExpireDate, no IsActive check
            var gymTotal = customers.Count;
            var gymActive = customerMemberships.Count(x =>
                x.LatestMembership!.ExpireDate >= today);
            var gymExpired = customerMemberships.Count(x =>
                x.LatestMembership!.ExpireDate < today);
            var gymExpiringSoon = customerMemberships.Count(x =>
                x.LatestMembership!.ExpireDate >= today &&
                x.LatestMembership.ExpireDate <= today.AddDays(7));
            var gymJoinedThisMonth = customers.Count(c => c.JoinDate >= monthStart);

            // Boxing stats
            var boxingMembers = (await _boxingRepository.GetAllAsync()).ToList();
            var boxingTotal = boxingMembers.Count;
            var boxingPaid = boxingMembers.Count(b => b.DueAmount == 0);
            var boxingWithDue = boxingMembers.Count(b => b.DueAmount > 0);
            var boxingTotalDue = boxingMembers.Sum(b => b.DueAmount);

            return new DashboardStats
            {
                GymTotal = gymTotal,
                GymActive = gymActive,
                GymExpired = gymExpired,
                GymExpiringSoon = gymExpiringSoon,
                GymJoinedThisMonth = gymJoinedThisMonth,
                BoxingTotal = boxingTotal,
                BoxingPaid = boxingPaid,
                BoxingWithDue = boxingWithDue,
                BoxingTotalDue = boxingTotalDue
            };
        }
    }
}
