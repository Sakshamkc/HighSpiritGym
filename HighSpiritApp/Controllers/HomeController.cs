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

            // TOTAL CUSTOMERS
            ViewBag.Total = await _context.Customers.CountAsync();

            // LOAD MEMBERSHIPS FIRST (ENTITY ROOT)
            var memberships = await _context.CustomerMemberships
                .Include(m => m.Customer)
                .Where(m => m.ExpireDate != null)
                .ToListAsync();

            // LATEST MEMBERSHIP PER CUSTOMER (IN MEMORY)
            var latestMemberships = memberships
                .GroupBy(m => m.CustomerID)
                .Select(g => g.OrderByDescending(x => x.StartDate).First())
                .ToList();

            // DASHBOARD COUNTS
            ViewBag.Active = latestMemberships.Count(m => m.ExpireDate >= today);
            ViewBag.Expired = latestMemberships.Count(m => m.ExpireDate < today);
            ViewBag.ExpiringSoon = latestMemberships.Count(m =>
                m.ExpireDate >= today && m.ExpireDate <= today.AddDays(7));

            ViewBag.JoinedToday = await _context.Customers
                .CountAsync(c => c.JoinDate.Date == today);

            // 🔔 BELL DATA (TOP 5 EXPIRED)
            ViewBag.ExpiredCount = ViewBag.Expired;

            ViewBag.ExpiredList = latestMemberships
                .Where(m => m.ExpireDate < today)
                .OrderBy(m => m.ExpireDate)
                .Take(5)
                .ToList();

            // EXPIRING SOON TABLE (MODEL)
            var expiringList = latestMemberships
                .Where(m =>
                    m.ExpireDate >= today &&
                    m.ExpireDate <= today.AddDays(7))
                .OrderBy(m => m.ExpireDate)
                .Take(5)
                .ToList();

            return View(expiringList);
        }


    }
}
