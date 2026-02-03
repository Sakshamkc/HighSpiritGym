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

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var dueMembers = (await _boxingService.GetMembersWithDueAsync()).ToList();

            ViewBag.DueCount = dueMembers.Count;

            return View(dueMembers.Take(5).ToList());
        }
    }
}
