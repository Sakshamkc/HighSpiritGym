using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.ViewComponents
{
    /// <summary>
    /// ViewComponent for displaying expired locker notifications
    /// </summary>
    public class LockerExpiredNotificationViewComponent : ViewComponent
    {
        private readonly ILockerService _lockerService;

        public LockerExpiredNotificationViewComponent(ILockerService lockerService)
        {
            _lockerService = lockerService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var expiredLockers = (await _lockerService.GetExpiredLockersAsync()).ToList();

            ViewBag.ExpiredCount = expiredLockers.Count;

            return View(expiredLockers.Take(5).ToList());
        }
    }
}
