using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HighSpiritApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly GymDbContext _context;
        public HomeController(GymDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var memberships = _context.CustomerMemberships;

            var totalMembers = await _context.Customers.CountAsync();

            var activeMembers = await memberships
                .Where(m => m.DueDaysComputed == 0)
                .CountAsync();

            var expiredMembers = await memberships
                .Where(m => m.DueDaysComputed > 0)
                .CountAsync();

            var expiringSoon = await memberships
                .Where(m =>
                    m.ExpireDate >= today &&
                    m.ExpireDate <= today.AddDays(7))
                .CountAsync();

            var joinedToday = await _context.Customers
                .Where(c => c.JoinDate.Date == today)
                .CountAsync();

            ViewBag.Total = totalMembers;
            ViewBag.Active = activeMembers;
            ViewBag.Expired = expiredMembers;
            ViewBag.ExpiringSoon = expiringSoon;
            ViewBag.JoinedToday = joinedToday;

            var expiringList = await memberships
                .Include(m => m.Customer)
                .Where(m =>
                    m.ExpireDate >= today &&
                    m.ExpireDate <= today.AddDays(7))
                .OrderBy(m => m.ExpireDate)
                .Take(5)
                .ToListAsync();

            return View(expiringList);
        }
    }
}
