using HighSpiritApp.Models.Boxing;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace HighSpiritApp.ViewComponents
{
    /// <summary>
    /// ViewComponent for displaying boxing members with due amount notifications
    /// </summary>
    public class BoxingDueNotificationViewComponent : ViewComponent
    {
        private readonly IBoxingService _boxingService;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "BoxingDueMembers_All";

        public BoxingDueNotificationViewComponent(IBoxingService boxingService, IMemoryCache cache)
        {
            _boxingService = boxingService;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? category = null)
        {
            var cached = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                return (await _boxingService.GetMembersWithDueAsync()).ToList();
            });
            var dueMembers = cached?.ToList() ?? new List<BoxingMember>();

            // Filter by category if specified
            if (!string.IsNullOrEmpty(category))
            {
                dueMembers = dueMembers
                    .Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewBag.DueCount = dueMembers.Count;
            ViewBag.Category = category;

            return View(dueMembers.Take(5).ToList());
        }
    }
}
