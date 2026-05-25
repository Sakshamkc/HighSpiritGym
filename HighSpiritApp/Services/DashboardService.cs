using HighSpiritApp.DataContext;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
        private readonly ILockerRepository _lockerRepository;
        private readonly GymDbContext _db;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "DashboardStats";

        public DashboardService(
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

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var cached = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                return await ComputeStatsAsync();
            });
            return cached!;
        }

        private async Task<DashboardStats> ComputeStatsAsync()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var weekAhead = today.AddDays(7);

            // === Gym stats: get latest membership per customer ===
            // Load only the fields we need, then compute "latest per customer" in memory.
            var allMemberships = await _db.CustomerMemberships.AsNoTracking()
                .Select(m => new { m.CustomerID, m.StartDate, m.ExpireDate, m.IsOnHold, m.DueAmount })
                .ToListAsync();

            var latestMemberships = allMemberships
                .GroupBy(m => m.CustomerID)
                .Select(g => g.OrderByDescending(x => x.StartDate).First())
                .ToList();

            var gymTotal = await _db.Customers.AsNoTracking().CountAsync();
            var gymJoinedThisMonth = await _db.Customers.AsNoTracking()
                .CountAsync(c => c.JoinDate >= monthStart);

            var gymActive = latestMemberships.Count(m => m.ExpireDate >= today || m.IsOnHold);
            var gymExpired = latestMemberships.Count(m => m.ExpireDate < today && !m.IsOnHold);
            var gymExpiringSoon = latestMemberships.Count(m =>
                !m.IsOnHold && m.ExpireDate >= today && m.ExpireDate <= weekAhead);
            var gymTotalDue = latestMemberships.Sum(m => m.DueAmount);

            // === Locker stats: aggregate in SQL ===
            var lockerStats = await _db.Lockers.AsNoTracking()
                .GroupBy(l => l.Gender)
                .Select(g => new
                {
                    Gender = g.Key,
                    Total = g.Count(),
                    Occupied = g.Count(l => l.Status == "Occupied" && l.AssignedTo != null && l.AssignedTo != "" && (l.EndDate == null || l.EndDate >= today)),
                    Empty = g.Count(l => l.Status == "Empty" || (l.Status != "Occupied" && l.Status != "Locked" && (l.AssignedTo == null || l.AssignedTo == ""))),
                    Expired = g.Count(l => l.Status == "Occupied" && l.EndDate != null && l.EndDate < today)
                })
                .ToListAsync();

            var gents = lockerStats.FirstOrDefault(x => x.Gender == "Gents");
            var ladies = lockerStats.FirstOrDefault(x => x.Gender == "Ladies");

            var lockerGentsTotal = gents?.Total ?? 0;
            var lockerGentsOccupied = gents?.Occupied ?? 0;
            var lockerGentsEmpty = gents?.Empty ?? 0;
            var lockerGentsExpired = gents?.Expired ?? 0;

            var lockerLadiesTotal = ladies?.Total ?? 0;
            var lockerLadiesOccupied = ladies?.Occupied ?? 0;
            var lockerLadiesEmpty = ladies?.Empty ?? 0;
            var lockerLadiesExpired = ladies?.Expired ?? 0;

            var lockerTotalDue = await _db.Lockers.AsNoTracking().SumAsync(l => (decimal?)l.DueAmount) ?? 0m;

            // === Boxing stats: aggregate in SQL ===
            var boxingTotal = await _db.BoxingMembers.AsNoTracking().CountAsync();
            var boxingPaid = await _db.BoxingMembers.AsNoTracking().CountAsync(b => b.DueAmount == 0);
            var boxingWithDue = await _db.BoxingMembers.AsNoTracking().CountAsync(b => b.DueAmount > 0);
            var boxingTotalDue = await _db.BoxingMembers.AsNoTracking().SumAsync(b => (decimal?)b.DueAmount) ?? 0m;

            return new DashboardStats
            {
                GymTotal = gymTotal,
                GymActive = gymActive,
                GymExpired = gymExpired,
                GymExpiringSoon = gymExpiringSoon,
                GymJoinedThisMonth = gymJoinedThisMonth,
                GymTotalDue = gymTotalDue,

                LockerGentsTotal = lockerGentsTotal,
                LockerGentsOccupied = lockerGentsOccupied,
                LockerGentsEmpty = lockerGentsEmpty,
                LockerGentsExpired = lockerGentsExpired,
                LockerLadiesTotal = lockerLadiesTotal,
                LockerLadiesOccupied = lockerLadiesOccupied,
                LockerLadiesEmpty = lockerLadiesEmpty,
                LockerLadiesExpired = lockerLadiesExpired,
                LockerTotalDue = lockerTotalDue,

                BoxingTotal = boxingTotal,
                BoxingPaid = boxingPaid,
                BoxingWithDue = boxingWithDue,
                BoxingTotalDue = boxingTotalDue
            };
        }
    }
}
