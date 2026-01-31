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
            if (membership.ExpireDate < membership.StartDate)
            {
                ModelState.AddModelError(
                    "ExpireDate",
                    "Expire date cannot be earlier than start date."
                );
                return View(membership);
            }

            var lastMembership = await _context.CustomerMemberships
                .Where(m => m.CustomerID == membership.CustomerID && m.IsActive)
                .OrderByDescending(m => m.ExpireDate)
                .FirstOrDefaultAsync();

            if (lastMembership != null)
            {
                lastMembership.IsActive = false;

                if (lastMembership.ExpireDate >= DateTime.Today)
                {
                    membership.StartDate = lastMembership.ExpireDate.AddDays(1);
                }
                else
                {
                    membership.StartDate = DateTime.Today;
                }
            }
            else
            {
                membership.StartDate = DateTime.Today;
            }

            membership.IsActive = true;

            _context.CustomerMemberships.Add(membership);
            await _context.SaveChangesAsync();

            TempData["success"] = "Membership renewed successfully!";
            return RedirectToAction("Index", "Customers");
        }

    }
}
