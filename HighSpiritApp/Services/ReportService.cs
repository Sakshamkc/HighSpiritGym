using HighSpiritApp.DataContext;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly GymDbContext _db;
        private readonly IMemoryCache _cache;

        public ReportService(
            ICustomerRepository customerRepository,
            IMembershipRepository membershipRepository,
            IBoxingRepository boxingRepository,
            ILockerRepository lockerRepository,
            GymDbContext db,
            IMemoryCache cache)
        {
            _customerRepository = customerRepository;
            _membershipRepository = membershipRepository;
            _boxingRepository = boxingRepository;
            _lockerRepository = lockerRepository;
            _db = db;
            _cache = cache;
        }

        public async Task<RevenueReport> GetRevenueReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Cache full-range report for 60s (no date filter). Filtered queries bypass cache.
            if (startDate == null && endDate == null)
            {
                var cached = await _cache.GetOrCreateAsync("RevenueReport_All", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                    return await ComputeRevenueReportAsync(null, null);
                });
                return cached!;
            }
            return await ComputeRevenueReportAsync(startDate, endDate);
        }

        private async Task<RevenueReport> ComputeRevenueReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var report = new RevenueReport();
            var today = DateTime.Today;
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddDays(-1);

            // --- Build filtered membership query (SQL-side, no entity load) ---
            var memQuery = _db.CustomerMemberships.AsNoTracking().AsQueryable();
            if (startDate.HasValue) memQuery = memQuery.Where(m => m.StartDate >= startDate.Value);
            if (endDate.HasValue) memQuery = memQuery.Where(m => m.StartDate <= endDate.Value);

            // Gym Revenue (SQL SUM)
            report.GymRevenue = await memQuery.SumAsync(m => (decimal?)m.PaidPrice) ?? 0m;

            // Gym Due - only from latest membership per customer
            // Pull lightweight tuples (CustomerID, StartDate, DueAmount) and group in memory.
            // (MySQL provider can't translate GroupBy+OrderBy+First subquery.)
            var membershipTuples = await _db.CustomerMemberships.AsNoTracking()
                .Select(m => new { m.CustomerID, m.StartDate, m.DueAmount })
                .ToListAsync();
            report.GymDue = membershipTuples
                .GroupBy(m => m.CustomerID)
                .Select(g => g.OrderByDescending(x => x.StartDate).First().DueAmount)
                .Sum();
            report.TotalGymMembers = await _db.Customers.AsNoTracking().CountAsync();

            // --- Boxing (SQL aggregates, no entity load) ---
            report.BoxingRevenue = await _db.BoxingMembers.AsNoTracking()
                .SumAsync(b => (decimal?)(b.CashAmount + b.EsewaAmount)) ?? 0m;
            report.BoxingDue = await _db.BoxingMembers.AsNoTracking()
                .SumAsync(b => (decimal?)b.DueAmount) ?? 0m;
            report.TotalBoxingMembers = await _db.BoxingMembers.AsNoTracking().CountAsync();

            // --- Locker (SQL aggregates) ---
            report.LockerRevenue = await _db.Lockers.AsNoTracking().SumAsync(l => (decimal?)l.PaidAmount) ?? 0m;
            report.LockerDue = await _db.Lockers.AsNoTracking().SumAsync(l => (decimal?)l.DueAmount) ?? 0m;
            report.TotalLockers = await _db.Lockers.AsNoTracking().CountAsync();
            report.ActiveLockers = await _db.Lockers.AsNoTracking().CountAsync(l => l.Status == "Occupied");

            // Totals
            report.TotalRevenue = report.GymRevenue + report.BoxingRevenue + report.LockerRevenue;
            report.TotalDue = report.GymDue + report.BoxingDue + report.LockerDue;
            report.TotalCollected = report.TotalRevenue;

            // --- This Month / Last Month / Today (SQL SUM with date filters) ---
            var thisMonthGym = await _db.CustomerMemberships.AsNoTracking()
                .Where(m => m.StartDate >= thisMonthStart).SumAsync(m => (decimal?)m.PaidPrice) ?? 0m;
            var thisMonthBoxing = await _db.BoxingMembers.AsNoTracking()
                .Where(b => b.JoinDate.HasValue && b.JoinDate.Value >= thisMonthStart)
                .SumAsync(b => (decimal?)(b.CashAmount + b.EsewaAmount)) ?? 0m;
            var thisMonthLocker = await _db.Lockers.AsNoTracking()
                .Where(l => l.StartDate.HasValue && l.StartDate.Value >= thisMonthStart)
                .SumAsync(l => (decimal?)l.PaidAmount) ?? 0m;
            report.ThisMonthRevenue = thisMonthGym + thisMonthBoxing + thisMonthLocker;

            var lastMonthGym = await _db.CustomerMemberships.AsNoTracking()
                .Where(m => m.StartDate >= lastMonthStart && m.StartDate <= lastMonthEnd)
                .SumAsync(m => (decimal?)m.PaidPrice) ?? 0m;
            var lastMonthBoxing = await _db.BoxingMembers.AsNoTracking()
                .Where(b => b.JoinDate.HasValue && b.JoinDate.Value >= lastMonthStart && b.JoinDate.Value <= lastMonthEnd)
                .SumAsync(b => (decimal?)(b.CashAmount + b.EsewaAmount)) ?? 0m;
            var lastMonthLocker = await _db.Lockers.AsNoTracking()
                .Where(l => l.StartDate.HasValue && l.StartDate.Value >= lastMonthStart && l.StartDate.Value <= lastMonthEnd)
                .SumAsync(l => (decimal?)l.PaidAmount) ?? 0m;
            report.LastMonthRevenue = lastMonthGym + lastMonthBoxing + lastMonthLocker;

            if (report.LastMonthRevenue > 0)
            {
                report.RevenueGrowth = ((report.ThisMonthRevenue - report.LastMonthRevenue) / report.LastMonthRevenue) * 100;
            }
            else if (report.ThisMonthRevenue > 0)
            {
                report.RevenueGrowth = 100;
            }

            // Today's stats
            var tomorrow = today.AddDays(1);
            var todayGymRev = await _db.CustomerMemberships.AsNoTracking()
                .Where(m => m.StartDate >= today && m.StartDate < tomorrow)
                .SumAsync(m => (decimal?)m.PaidPrice) ?? 0m;
            var todayGymCount = await _db.CustomerMemberships.AsNoTracking()
                .CountAsync(m => m.StartDate >= today && m.StartDate < tomorrow);
            var todayBoxRev = await _db.BoxingMembers.AsNoTracking()
                .Where(b => b.JoinDate.HasValue && b.JoinDate.Value >= today && b.JoinDate.Value < tomorrow)
                .SumAsync(b => (decimal?)(b.CashAmount + b.EsewaAmount)) ?? 0m;
            var todayBoxCount = await _db.BoxingMembers.AsNoTracking()
                .CountAsync(b => b.JoinDate.HasValue && b.JoinDate.Value >= today && b.JoinDate.Value < tomorrow);
            var todayLockRev = await _db.Lockers.AsNoTracking()
                .Where(l => l.StartDate.HasValue && l.StartDate.Value >= today && l.StartDate.Value < tomorrow)
                .SumAsync(l => (decimal?)l.PaidAmount) ?? 0m;
            var todayLockCount = await _db.Lockers.AsNoTracking()
                .CountAsync(l => l.StartDate.HasValue && l.StartDate.Value >= today && l.StartDate.Value < tomorrow);

            report.TodayRevenue = todayGymRev + todayBoxRev + todayLockRev;
            report.TodayTransactions = todayGymCount + todayBoxCount + todayLockCount;

            return report;
        }

        public async Task<MonthlyRevenueData> GetMonthlyRevenueAsync(int year)
        {
            var cached = await _cache.GetOrCreateAsync($"MonthlyRevenue_{year}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                return await ComputeMonthlyRevenueAsync(year);
            });
            return cached!;
        }

        private async Task<MonthlyRevenueData> ComputeMonthlyRevenueAsync(int year)
        {
            var data = new MonthlyRevenueData { Year = year };

            // SQL-side group-by-month aggregates (only return 12 rows max per category)
            var gymMonthly = await _db.CustomerMemberships.AsNoTracking()
                .Where(m => m.StartDate.Year == year)
                .GroupBy(m => m.StartDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(m => (decimal?)m.PaidPrice) ?? 0m })
                .ToListAsync();

            var boxingMonthly = await _db.BoxingMembers.AsNoTracking()
                .Where(b => b.JoinDate.HasValue && b.JoinDate.Value.Year == year)
                .GroupBy(b => b.JoinDate!.Value.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(b => (decimal?)(b.CashAmount + b.EsewaAmount)) ?? 0m })
                .ToListAsync();

            var lockerMonthly = await _db.Lockers.AsNoTracking()
                .Where(l => l.StartDate.HasValue && l.StartDate.Value.Year == year)
                .GroupBy(l => l.StartDate!.Value.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(l => (decimal?)l.PaidAmount) ?? 0m })
                .ToListAsync();

            for (int month = 1; month <= 12; month++)
            {
                data.Months.Add(new MonthRevenue
                {
                    Month = month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                    GymRevenue = gymMonthly.FirstOrDefault(x => x.Month == month)?.Total ?? 0m,
                    BoxingRevenue = boxingMonthly.FirstOrDefault(x => x.Month == month)?.Total ?? 0m,
                    LockerRevenue = lockerMonthly.FirstOrDefault(x => x.Month == month)?.Total ?? 0m
                });
            }

            data.YearTotal = data.Months.Sum(m => m.Total);
            return data;
        }

        public async Task<List<RecentTransaction>> GetRecentTransactionsAsync(int count = 20)
        {
            var transactions = new List<RecentTransaction>();

            // Gym memberships - project without Photo blob
            var memberships = await _db.CustomerMemberships.AsNoTracking()
                .OrderByDescending(m => m.StartDate)
                .Take(count)
                .Select(m => new
                {
                    m.MembershipID,
                    CustomerName = m.Customer != null ? m.Customer.FullName : null,
                    m.PlanName,
                    m.PaidPrice,
                    m.StartDate,
                    m.DueAmount
                })
                .ToListAsync();

            foreach (var m in memberships)
            {
                transactions.Add(new RecentTransaction
                {
                    Id = m.MembershipID,
                    Type = "Gym",
                    MemberName = m.CustomerName ?? "Unknown",
                    Description = m.PlanName ?? "Membership",
                    Amount = m.PaidPrice,
                    Date = m.StartDate,
                    Status = m.DueAmount > 0 ? "Partial" : "Paid"
                });
            }

            // Boxing - no Photo
            var boxingMembers = await _db.BoxingMembers.AsNoTracking()
                .Where(b => b.JoinDate.HasValue)
                .OrderByDescending(b => b.JoinDate)
                .Take(count)
                .Select(b => new
                {
                    b.BoxingMemberID,
                    b.Name,
                    b.PerMonthClass,
                    b.CashAmount,
                    b.EsewaAmount,
                    b.JoinDate,
                    b.DueAmount
                })
                .ToListAsync();

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

            // Locker rentals
            var lockers = await _db.Lockers.AsNoTracking()
                .Where(l => l.StartDate.HasValue && l.Status == "Occupied")
                .OrderByDescending(l => l.StartDate)
                .Take(count)
                .Select(l => new
                {
                    l.LockerID,
                    l.LockerNumber,
                    l.AssignedTo,
                    l.RentalMonths,
                    l.PaidAmount,
                    l.StartDate,
                    l.DueAmount
                })
                .ToListAsync();

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

    // This Month Stats - include Gym + Boxing + Locker
    var thisMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var lastMonthStart = thisMonthStart.AddMonths(-1);
    var lastMonthEnd = thisMonthStart.AddDays(-1);

    var thisMonthGym = memberships.Where(m => m.StartDate >= thisMonthStart).Sum(m => m.PaidPrice);
    var thisMonthBoxing = boxingMembers.Where(b => b.JoinDate.HasValue && b.JoinDate.Value >= thisMonthStart).Sum(b => b.CashAmount + b.EsewaAmount);
    var thisMonthLocker = lockers.Where(l => l.StartDate.HasValue && l.StartDate.Value >= thisMonthStart).Sum(l => l.PaidAmount);
    report.ThisMonthRevenue = thisMonthGym + thisMonthBoxing + thisMonthLocker;

    var lastMonthGym = memberships.Where(m => m.StartDate >= lastMonthStart && m.StartDate <= lastMonthEnd).Sum(m => m.PaidPrice);
    var lastMonthBoxing = boxingMembers.Where(b => b.JoinDate.HasValue && b.JoinDate.Value >= lastMonthStart && b.JoinDate.Value <= lastMonthEnd).Sum(b => b.CashAmount + b.EsewaAmount);
    var lastMonthLocker = lockers.Where(l => l.StartDate.HasValue && l.StartDate.Value >= lastMonthStart && l.StartDate.Value <= lastMonthEnd).Sum(l => l.PaidAmount);
    report.LastMonthRevenue = lastMonthGym + lastMonthBoxing + lastMonthLocker;

    // Calculate growth percentage
    if (report.LastMonthRevenue > 0)
    {
        report.RevenueGrowth = ((report.ThisMonthRevenue - report.LastMonthRevenue) / report.LastMonthRevenue) * 100;
    }
    else if (report.ThisMonthRevenue > 0)
    {
        report.RevenueGrowth = 100;
    }

    // Today's Stats - include Gym + Boxing + Locker
    var today = DateTime.Today;
    var todayGymList = memberships.Where(m => m.StartDate.Date == today).ToList();
    var todayBoxingList = boxingMembers.Where(b => b.JoinDate.HasValue && b.JoinDate.Value.Date == today).ToList();
    var todayLockerList = lockers.Where(l => l.StartDate.HasValue && l.StartDate.Value.Date == today).ToList();

    report.TodayRevenue = todayGymList.Sum(m => m.PaidPrice)
                        + todayBoxingList.Sum(b => b.CashAmount + b.EsewaAmount)
                        + todayLockerList.Sum(l => l.PaidAmount);
    report.TodayTransactions = todayGymList.Count + todayBoxingList.Count + todayLockerList.Count;

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
