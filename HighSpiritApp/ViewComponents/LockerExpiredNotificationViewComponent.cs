using HighSpiritApp.Models.Locker;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace HighSpiritApp.ViewComponents
{
    /// <summary>
    /// ViewComponent for displaying expired locker notifications
    /// </summary>
    public class LockerExpiredNotificationViewComponent : ViewComponent
    {
        private readonly ILockerService _lockerService;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "ExpiredLockers_All";

        public LockerExpiredNotificationViewComponent(ILockerService lockerService, IMemoryCache cache)
        {
            _lockerService = lockerService;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? gender = null)
        {
            var cached = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                return (await _lockerService.GetExpiredLockersAsync()).ToList();
            });
            var expiredLockers = cached?.ToList() ?? new List<Locker>();

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
