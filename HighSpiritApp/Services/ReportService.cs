using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;
using System.Globalization;

namespace HighSpiritApp.Services
{
    /// <summary>
    /// Report generation service implementation
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IMembershipRepository _membershipRepository;
        private readonly IBoxingRepository _boxingRepository;
        private readonly ILockerRepository _lockerRepository;

        public ReportService(
            ICustomerRepository customerRepository,
            IMembershipRepository membershipRepository,
            IBoxingRepository boxingRepository,
            ILockerRepository lockerRepository)
        {
            _customerRepository = customerRepository;
            _membershipRepository = membershipRepository;
            _boxingRepository = boxingRepository;
            _lockerRepository = lockerRepository;
        }

        public async Task<RevenueReport> GetRevenueReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var report = new RevenueReport();

            // Get all data
            var memberships = (await _membershipRepository.GetAllAsync()).ToList();
            var boxingMembers = (await _boxingRepository.GetAllAsync()).ToList();
            var lockers = (await _lockerRepository.GetAllAsync()).ToList();

            // Filter by date if provided
            if (startDate.HasValue)
            {
                memberships = memberships.Where(m => m.StartDate >= startDate.Value).ToList();
            }
            if (endDate.HasValue)
            {
                memberships = memberships.Where(m => m.StartDate <= endDate.Value).ToList();
            }

            // Gym Revenue
            report.GymRevenue = memberships.Sum(m => m.PaidPrice);

            // Gym Due - only from latest membership per customer (matches Dashboard logic)
            var customers = (await _customerRepository.GetAllWithMembershipsAsync()).ToList();
            report.GymDue = customers
                .Select(c => c.Memberships?.OrderByDescending(m => m.StartDate).FirstOrDefault())
                .Where(m => m != null)
                .Sum(m => m!.DueAmount);
            report.TotalGymMembers = customers.Count;

            // Boxing Revenue
            report.BoxingRevenue = boxingMembers.Sum(b => b.CashAmount + b.EsewaAmount);
            report.BoxingDue = boxingMembers.Sum(b => b.DueAmount);
            report.TotalBoxingMembers = boxingMembers.Count;

            // Locker Revenue
            report.LockerRevenue = lockers.Sum(l => l.PaidAmount);
            report.LockerDue = lockers.Sum(l => l.DueAmount);
            report.TotalLockers = lockers.Count;
            report.ActiveLockers = lockers.Count(l => l.Status == "Occupied");

            // Totals
            report.TotalRevenue = report.GymRevenue + report.BoxingRevenue + report.LockerRevenue;
            report.TotalDue = report.GymDue + report.BoxingDue + report.LockerDue;
            report.TotalCollected = report.TotalRevenue;

            // This Month Stats
            var thisMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddDays(-1);

            var thisMonthMemberships = memberships.Where(m => m.StartDate >= thisMonthStart).ToList();
            report.ThisMonthRevenue = thisMonthMemberships.Sum(m => m.PaidPrice);

            var lastMonthMemberships = memberships.Where(m => m.StartDate >= lastMonthStart && m.StartDate <= lastMonthEnd).ToList();
            report.LastMonthRevenue = lastMonthMemberships.Sum(m => m.PaidPrice);

            // Calculate growth percentage
            if (report.LastMonthRevenue > 0)
            {
                report.RevenueGrowth = ((report.ThisMonthRevenue - report.LastMonthRevenue) / report.LastMonthRevenue) * 100;
            }
            else if (report.ThisMonthRevenue > 0)
            {
                report.RevenueGrowth = 100;
            }

            // Today's Stats
            var today = DateTime.Today;
            var todayMemberships = memberships.Where(m => m.StartDate.Date == today).ToList();
            report.TodayRevenue = todayMemberships.Sum(m => m.PaidPrice);
            report.TodayTransactions = todayMemberships.Count;

            return report;
        }

        public async Task<MonthlyRevenueData> GetMonthlyRevenueAsync(int year)
        {
            var data = new MonthlyRevenueData { Year = year };

            var memberships = (await _membershipRepository.GetAllAsync())
                .Where(m => m.StartDate.Year == year)
                .ToList();

            var boxingMembers = (await _boxingRepository.GetAllAsync())
                .Where(b => b.JoinDate.HasValue && b.JoinDate.Value.Year == year)
                .ToList();

            var lockers = (await _lockerRepository.GetAllAsync())
                .Where(l => l.StartDate.HasValue && l.StartDate.Value.Year == year)
                .ToList();

            for (int month = 1; month <= 12; month++)
            {
                var monthRevenue = new MonthRevenue
                {
                    Month = month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                    GymRevenue = memberships.Where(m => m.StartDate.Month == month).Sum(m => m.PaidPrice),
                    BoxingRevenue = boxingMembers.Where(b => b.JoinDate.HasValue && b.JoinDate.Value.Month == month).Sum(b => b.CashAmount + b.EsewaAmount),
                    LockerRevenue = lockers.Where(l => l.StartDate.HasValue && l.StartDate.Value.Month == month).Sum(l => l.PaidAmount)
                };

                data.Months.Add(monthRevenue);
            }

            data.YearTotal = data.Months.Sum(m => m.Total);

            return data;
        }

        public async Task<List<RecentTransaction>> GetRecentTransactionsAsync(int count = 20)
        {
            var transactions = new List<RecentTransaction>();

            // Get gym memberships
            var memberships = (await _membershipRepository.GetAllWithCustomerAsync())
                .OrderByDescending(m => m.StartDate)
                .Take(count)
                .ToList();

            foreach (var m in memberships)
            {
                transactions.Add(new RecentTransaction
                {
                    Id = m.MembershipID,
                    Type = "Gym",
                    MemberName = m.Customer?.FullName ?? "Unknown",
                    Description = m.PlanName ?? "Membership",
                    Amount = m.PaidPrice,
                    Date = m.StartDate,
                    Status = m.DueAmount > 0 ? "Partial" : "Paid"
                });
            }

            // Get boxing payments
            var boxingMembers = (await _boxingRepository.GetAllAsync())
                .Where(b => b.JoinDate.HasValue)
                .OrderByDescending(b => b.JoinDate)
                .Take(count)
                .ToList();

            foreach (var b in boxingMembers)
            {
                transactions.Add(new RecentTransaction
                {
                    Id = b.BoxingMemberID,
                    Type = "Boxing",
                    MemberName = b.Name,
                    Description = $"Boxing - {b.PerMonthClass}",
                    Amount = b.CashAmount + b.EsewaAmount,
                    Date = b.JoinDate ?? DateTime.Today,
                    Status = b.DueAmount > 0 ? "Partial" : "Paid"
                });
            }

            // Get locker rentals
            var lockers = (await _lockerRepository.GetAllAsync())
                .Where(l => l.StartDate.HasValue && l.Status == "Occupied")
                .OrderByDescending(l => l.StartDate)
                .Take(count)
                .ToList();

            foreach (var l in lockers)
            {
                transactions.Add(new RecentTransaction
                {
                    Id = l.LockerID,
                    Type = "Locker",
                    MemberName = l.AssignedTo ?? "Unknown",
                    Description = $"Locker {l.LockerNumber} ({l.RentalMonths} months)",
                    Amount = l.PaidAmount,
                    Date = l.StartDate ?? DateTime.Today,
                    Status = l.DueAmount > 0 ? "Partial" : "Paid"
                });
            }

            // Sort by date and take top count
            return transactions
                .OrderByDescending(t => t.Date)
                .Take(count)
                .ToList();
        }

        public async Task<CategoryWiseRevenue> GetCategoryWiseRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var report = await GetRevenueReportAsync(startDate, endDate);

            var total = report.GymRevenue + report.BoxingRevenue + report.LockerRevenue;

            return new CategoryWiseRevenue
            {
                GymAmount = report.GymRevenue,
                BoxingAmount = report.BoxingRevenue,
                LockerAmount = report.LockerRevenue,
                Total = total,
                GymPercentage = total > 0 ? (report.GymRevenue / total) * 100 : 0,
                BoxingPercentage = total > 0 ? (report.BoxingRevenue / total) * 100 : 0,
                LockerPercentage = total > 0 ? (report.LockerRevenue / total) * 100 : 0
            };
        }
    }
}
