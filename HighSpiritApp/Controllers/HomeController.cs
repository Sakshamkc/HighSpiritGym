using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HighSpiritApp.Controllers
{
    [Authorize]
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

            // TOTAL CUSTOMERS (always from Customers table)
            var totalMembers = await _context.Customers.CountAsync();

            // ACTIVE CUSTOMERS
            var activeMembers = await _context.Customers
                .Where(c => c.Memberships.Any(m =>
                    m.IsActive && m.DueDaysComputed == 0))
                .CountAsync();

            // EXPIRED CUSTOMERS
            var expiredMembers = await _context.Customers
                .Where(c => c.Memberships.Any(m =>
                    m.IsActive && m.DueDaysComputed > 0))
                .CountAsync();

            // EXPIRING SOON (7 DAYS)
            var expiringSoon = await _context.Customers
                .Where(c => c.Memberships.Any(m =>
                    m.IsActive &&
                    m.ExpireDate >= today &&
                    m.ExpireDate <= today.AddDays(7)))
                .CountAsync();

            // JOINED TODAY
            var joinedToday = await _context.Customers
                .Where(c => c.JoinDate.Date == today)
                .CountAsync();

            ViewBag.Total = totalMembers;
            ViewBag.Active = activeMembers;
            ViewBag.Expired = expiredMembers;
            ViewBag.ExpiringSoon = expiringSoon;
            ViewBag.JoinedToday = joinedToday;

            var expiringList = await _context.CustomerMemberships
     .Include(m => m.Customer)
     .Where(m =>
         m.IsActive &&
         m.ExpireDate >= today &&
         m.ExpireDate <= today.AddDays(7))
     .OrderBy(m => m.ExpireDate)
     .Take(5)
     .ToListAsync();

            return View(expiringList);
        }
    }
}
