using HighSpiritApp.Models;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    /// <summary>
    /// Memberships controller - Membership management and renewal
    /// </summary>
    [Authorize]
    public class MembershipsController : Controller
    {
        private readonly IMembershipService _membershipService;

        public MembershipsController(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        public IActionResult Create(int customerId)
        {
            ViewBag.CustomerID = customerId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerMembership membership)
        {
            await _membershipService.CreateAsync(membership);
            return RedirectToAction("Index", "Customers");
        }

        public async Task<IActionResult> Renew(int id) // id = CustomerID
        {
            try
            {
                var renewalInfo = await _membershipService.GetRenewalInfoAsync(id);

                ViewBag.CustomerName = renewalInfo.CustomerName;
                ViewBag.LastExpireDate = renewalInfo.LastExpireDate;

                return View(new CustomerMembership
                {
                    CustomerID = id,
                    StartDate = renewalInfo.SuggestedStartDate,
                    Duration = 1,
                    DueAmount = 0,
                    PlanName = renewalInfo.LastPlanName
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Renew(CustomerMembership membership, int DueAmount)
        {
            try
            {
                membership.DueAmount = DueAmount;
                await _membershipService.RenewAsync(membership);
                TempData["success"] = "Membership renewed successfully!";
                return RedirectToAction("Index", "Customers");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("ExpireDate", ex.Message);
                return View(membership);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, int customerId)
        {
            try
            {
                await _membershipService.DeleteAsync(id);
                TempData["success"] = "Membership record deleted successfully.";
            }
            catch (KeyNotFoundException)
            {
                TempData["error"] = "Membership record not found.";
            }
            return RedirectToAction("Details", "Customers", new { id = customerId });
        }
    }
}
