using HighSpiritApp.DataContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.ViewComponents
{
    public class BoxingDueNotificationViewComponent: ViewComponent
    {
        private readonly GymDbContext _context;

        public BoxingDueNotificationViewComponent(GymDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var dueList = await _context.BoxingMembers
                .Where(b => b.DueAmount > 0)
                .OrderByDescending(b => b.DueAmount)
                .Take(5)
                .ToListAsync();

            ViewBag.DueCount = await _context.BoxingMembers
                .CountAsync(b => b.DueAmount > 0);

            return View(dueList);
        }
    }
}
