using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    /// <summary>
    /// Home controller - Dashboard
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IMembershipService _membershipService;
        private readonly IBoxingService _boxingService;
        private readonly ILockerService _lockerService;

        public HomeController(
            IDashboardService dashboardService,
            IMembershipService membershipService,
            IBoxingService boxingService,
            ILockerService lockerService)
        {
            _dashboardService = dashboardService;
            _membershipService = membershipService;
            _boxingService = boxingService;
            _lockerService = lockerService;
        }

        public async Task<IActionResult> Index()
        {
            // Get dashboard statistics
            var stats = await _dashboardService.GetDashboardStatsAsync();

            // Revenue stats
            ViewBag.TotalRevenue = stats.TotalRevenue;
            ViewBag.GymRevenue = stats.GymRevenue;
            ViewBag.LockerRevenue = stats.LockerRevenue;
            ViewBag.BoxingRevenue = stats.BoxingRevenue;
            ViewBag.TotalDueAmount = stats.TotalDueAmount;
            ViewBag.MonthlyRevenue = stats.MonthlyRevenue;

            // Gym stats
            ViewBag.Total = stats.GymTotal;
            ViewBag.Active = stats.GymActive;
            ViewBag.Expired = stats.GymExpired;
            ViewBag.ExpiringSoon = stats.GymExpiringSoon;
            ViewBag.JoinedThisMonth = stats.GymJoinedThisMonth;
            ViewBag.GymTotalDue = stats.GymTotalDue;

            // Locker stats
            ViewBag.LockerGentsTotal = stats.LockerGentsTotal;
            ViewBag.LockerGentsOccupied = stats.LockerGentsOccupied;
            ViewBag.LockerGentsEmpty = stats.LockerGentsEmpty;
            ViewBag.LockerGentsExpired = stats.LockerGentsExpired;
            ViewBag.LockerLadiesTotal = stats.LockerLadiesTotal;
            ViewBag.LockerLadiesOccupied = stats.LockerLadiesOccupied;
            ViewBag.LockerLadiesEmpty = stats.LockerLadiesEmpty;
            ViewBag.LockerLadiesExpired = stats.LockerLadiesExpired;
            ViewBag.LockerTotalDue = stats.LockerTotalDue;

            // Boxing stats
            ViewBag.BoxingTotal = stats.BoxingTotal;
            ViewBag.BoxingPaid = stats.BoxingPaid;
            ViewBag.BoxingWithDue = stats.BoxingWithDue;
            ViewBag.BoxingDue = stats.BoxingTotalDue;

            // Get lists for tables
            var expiringList = (await _membershipService.GetExpiringSoonAsync(7)).Take(5).ToList();
            ViewBag.BoxingDueList = (await _boxingService.GetMembersWithDueAsync()).Take(5).ToList();
            ViewBag.LockerExpiredList = (await _lockerService.GetExpiredLockersAsync()).Take(5).ToList();

            return View(expiringList);
        }
    }
}
