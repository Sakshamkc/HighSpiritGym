using HighSpiritApp.DataContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ExpiredNotificationViewComponent : ViewComponent
{
    private readonly GymDbContext _context;

    public ExpiredNotificationViewComponent(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var today = DateTime.Today;

        var memberships = await _context.CustomerMemberships
            .Include(m => m.Customer)
            .Where(m => m.ExpireDate != null)
            .ToListAsync();

        var latestMemberships = memberships
            .GroupBy(m => m.CustomerID)
            .Select(g => g.OrderByDescending(x => x.StartDate).First())
            .Where(m => m.ExpireDate < today)
            .OrderBy(m => m.ExpireDate)
            .ToList();

        return View(latestMemberships);
    }
}
