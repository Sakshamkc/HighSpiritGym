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
        private readonly IActivityLogService _activityLogService;

        public MembershipsController(IMembershipService membershipService, IActivityLogService activityLogService)
        {
            _membershipService = membershipService;
            _activityLogService = activityLogService;
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
            await _activityLogService.LogAsync("Created", "Membership", membership.MembershipID, $"Customer #{membership.CustomerID}", $"Created {membership.PlanName} membership for customer #{membership.CustomerID}", User.Identity?.Name ?? "Admin");
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
                await _activityLogService.LogAsync("Renewed", "Membership", membership.MembershipID, $"Customer #{membership.CustomerID}", $"Renewed {membership.PlanName} membership for customer #{membership.CustomerID}", User.Identity?.Name ?? "Admin");
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
                await _activityLogService.LogAsync("Deleted", "Membership", id, $"Customer #{customerId}", $"Deleted membership #{id} for customer #{customerId}", User.Identity?.Name ?? "Admin");
                TempData["success"] = "Membership record deleted successfully.";
            }
            catch (KeyNotFoundException)
            {
                TempData["error"] = "Membership record not found.";
            }
            return RedirectToAction("Details", "Customers", new { id = customerId });
        }

        [HttpPost]
        public async Task<IActionResult> Hold(int id, int customerId, string? returnUrl = null)
        {
            try
            {
                await _membershipService.HoldAsync(id);
                TempData["success"] = "Membership has been put on hold.";
            }
            catch (KeyNotFoundException)
            {
                TempData["error"] = "Membership not found.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
            }
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Details", "Customers", new { id = customerId });
        }

        [HttpPost]
        public async Task<IActionResult> Resume(int id, int customerId, string? returnUrl = null)
        {
            try
            {
                await _membershipService.ResumeAsync(id);
                TempData["success"] = "Membership has been resumed. Expire date extended by hold duration.";
            }
            catch (KeyNotFoundException)
            {
                TempData["error"] = "Membership not found.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
            }
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Details", "Customers", new { id = customerId });
        }
    }
}
