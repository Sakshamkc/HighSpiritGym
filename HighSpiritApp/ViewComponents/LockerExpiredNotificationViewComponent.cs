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

        public async Task<IViewComponentResult> InvokeAsync(string? gender = null)
        {
            var expiredLockers = (await _lockerService.GetExpiredLockersAsync()).ToList();

            // Filter by gender if specified
            if (!string.IsNullOrEmpty(gender))
            {
                expiredLockers = expiredLockers.Where(l => l.Gender == gender).ToList();
            }

            ViewBag.ExpiredCount = expiredLockers.Count;
            ViewBag.CurrentGender = gender;

            return View(expiredLockers.Take(5).ToList());
        }
    }
}
