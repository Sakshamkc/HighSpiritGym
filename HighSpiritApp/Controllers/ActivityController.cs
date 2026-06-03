using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    [Authorize]
    public class ActivityController : Controller
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        public async Task<IActionResult> Index(string? entityType, string? action, string? search, int page = 1)
        {
            int pageSize = 30;
            var (items, totalCount) = await _activityLogService.GetFilteredAsync(entityType, action, search, page, pageSize);

            ViewBag.CurrentEntityType = entityType;
            ViewBag.CurrentAction = action;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View(items);
        }
    }
}
