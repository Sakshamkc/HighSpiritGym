using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}
