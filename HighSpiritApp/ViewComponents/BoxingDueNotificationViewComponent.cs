using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.ViewComponents
{
    /// <summary>
    /// ViewComponent for displaying boxing members with due amount notifications
    /// </summary>
    public class BoxingDueNotificationViewComponent : ViewComponent
    {
        private readonly IBoxingService _boxingService;

        public BoxingDueNotificationViewComponent(IBoxingService boxingService)
        {
            _boxingService = boxingService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? category = null)
        {
            var dueMembers = (await _boxingService.GetMembersWithDueAsync()).ToList();

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
