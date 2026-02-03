using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.ViewComponents
{
    /// <summary>
    /// ViewComponent for displaying expired gym membership notifications
    /// </summary>
    public class ExpiredNotificationViewComponent : ViewComponent
    {
        private readonly IMembershipService _membershipService;

        public ExpiredNotificationViewComponent(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var expiredMemberships = await _membershipService.GetExpiredMembershipsAsync();
            return View(expiredMemberships.ToList());
        }
    }
}
