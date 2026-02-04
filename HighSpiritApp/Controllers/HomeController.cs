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

        public HomeController(
            IDashboardService dashboardService,
            IMembershipService membershipService,
            IBoxingService boxingService)
        {
            _dashboardService = dashboardService;
            _membershipService = membershipService;
            _boxingService = boxingService;
        }

        public async Task<IActionResult> Index()
        {
            // Get dashboard statistics
            var stats = await _dashboardService.GetDashboardStatsAsync();

            // Gym stats
            ViewBag.Total = stats.GymTotal;
            ViewBag.Active = stats.GymActive;
            ViewBag.Expired = stats.GymExpired;
            ViewBag.ExpiringSoon = stats.GymExpiringSoon;
            ViewBag.JoinedThisMonth = stats.GymJoinedThisMonth;
            ViewBag.GymTotalDue = stats.GymTotalDue;

            // Boxing stats
            ViewBag.BoxingTotal = stats.BoxingTotal;
            ViewBag.BoxingPaid = stats.BoxingPaid;
            ViewBag.BoxingWithDue = stats.BoxingWithDue;
            ViewBag.BoxingDue = stats.BoxingTotalDue;

            // Get lists for tables
            var expiringList = (await _membershipService.GetExpiringSoonAsync(7)).Take(5).ToList();
            ViewBag.BoxingDueList = (await _boxingService.GetMembersWithDueAsync()).Take(5).ToList();

            return View(expiringList);
        }
    }
}
