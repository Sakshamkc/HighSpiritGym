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
        private readonly ICustomerService _customerService;
        private readonly IActivityLogService _activityLogService;

        public MembershipsController(IMembershipService membershipService, ICustomerService customerService, IActivityLogService activityLogService)
        {
            _membershipService = membershipService;
            _customerService = customerService;
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
            var customer = await _customerService.GetByIdAsync(membership.CustomerID);
            var customerName = customer?.FullName ?? $"Customer #{membership.CustomerID}";
            await _activityLogService.LogAsync("Created", "Membership", membership.MembershipID, customerName, $"Created {membership.PlanName} membership for {customerName}", User.Identity?.Name ?? "Admin");
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
                var customer = await _customerService.GetByIdAsync(membership.CustomerID);
                var customerName = customer?.FullName ?? $"Customer #{membership.CustomerID}";
                await _activityLogService.LogAsync("Renewed", "Membership", membership.MembershipID, customerName, $"Renewed {membership.PlanName} membership for {customerName}", User.Identity?.Name ?? "Admin");
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
                var customer = await _customerService.GetByIdAsync(customerId);
                var customerName = customer?.FullName ?? $"Customer #{customerId}";
                await _membershipService.DeleteAsync(id);
                await _activityLogService.LogAsync("Deleted", "Membership", id, customerName, $"Deleted membership #{id} for {customerName}", User.Identity?.Name ?? "Admin");
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
