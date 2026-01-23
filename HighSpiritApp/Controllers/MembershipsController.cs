using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Controllers
{
    [Authorize]
    public class MembershipsController : Controller
    {
        private readonly GymDbContext _context;
        public MembershipsController(GymDbContext context)
        {
            _context = context;
        }

        public IActionResult Create(int customerId)
        {
            ViewBag.CustomerID = customerId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerMembership m)
        {
            m.IsActive = true;
            _context.CustomerMemberships.Add(m);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Customers");
        }
        public async Task<IActionResult> Renew(int id) // id = CustomerID
        {
            var customer = await _context.Customers
                .Include(c => c.Memberships)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null) return NotFound();

            return View(new CustomerMembership
            {
                CustomerID = id,
                StartDate = DateTime.Today,
                Duration = 1
            });
        }

        [HttpPost]
        public async Task<IActionResult> Renew(CustomerMembership membership)
        {
            // Deactivate old memberships
            var oldMemberships = _context.CustomerMemberships
                .Where(m => m.CustomerID == membership.CustomerID && m.IsActive);

            foreach (var m in oldMemberships)
                m.IsActive = false;

            // Add new membership
            membership.IsActive = true;
            membership.StartDate = DateTime.Today;

            _context.CustomerMemberships.Add(membership);
            await _context.SaveChangesAsync();

            TempData["success"] = "Membership renewed successfully!";
            return RedirectToAction("Index", "Customers");
        }

    }
}
