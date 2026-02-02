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

            // Get the last active membership to calculate suggested start date
            var lastMembership = customer.Memberships
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.ExpireDate)
                .FirstOrDefault();

            DateTime suggestedStartDate;
            
            if (lastMembership != null && lastMembership.ExpireDate >= DateTime.Today)
            {
                // If membership hasn't expired yet, new start = day after expire
                suggestedStartDate = lastMembership.ExpireDate.AddDays(1);
            }
            else
            {
                // If no active membership or already expired, start from today
                suggestedStartDate = DateTime.Today;
            }

            ViewBag.CustomerName = customer.FullName;
            ViewBag.LastExpireDate = lastMembership?.ExpireDate;

            return View(new CustomerMembership
            {
                CustomerID = id,
                StartDate = suggestedStartDate,
                Duration = 1,
                PlanName = lastMembership?.PlanName // Pre-fill with last plan
            });
        }

        [HttpPost]
        public async Task<IActionResult> Renew(CustomerMembership membership)
        {
            // Calculate expire date if not provided
            if (membership.ExpireDate == DateTime.MinValue || membership.ExpireDate == default)
            {
                membership.ExpireDate = membership.StartDate.AddMonths(membership.Duration);
            }

            if (membership.ExpireDate < membership.StartDate)
            {
                ModelState.AddModelError(
                    "ExpireDate",
                    "Expire date cannot be earlier than start date."
                );
                return View(membership);
            }

            // Deactivate previous active membership
            var lastMembership = await _context.CustomerMemberships
                .Where(m => m.CustomerID == membership.CustomerID && m.IsActive)
                .OrderByDescending(m => m.ExpireDate)
                .FirstOrDefaultAsync();

            if (lastMembership != null)
            {
                lastMembership.IsActive = false;
            }

            membership.IsActive = true;

            _context.CustomerMemberships.Add(membership);
            await _context.SaveChangesAsync();

            TempData["success"] = "Membership renewed successfully!";
            return RedirectToAction("Index", "Customers");
        }

    }
}
