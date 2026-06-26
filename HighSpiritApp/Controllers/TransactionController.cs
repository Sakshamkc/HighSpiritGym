using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class TransactionController : Controller
    {
        private readonly ITransactionLogService _transactionLogService;

        public TransactionController(ITransactionLogService transactionLogService)
        {
            _transactionLogService = transactionLogService;
        }

        public async Task<IActionResult> Index(string? entityType, string? transactionType, string? search,
            DateTime? from, DateTime? to, int page = 1)
        {
            int pageSize = 30;
            var (items, totalCount) = await _transactionLogService.GetFilteredAsync(
                entityType, transactionType, search, from, to, page, pageSize);

            ViewBag.EntityType = entityType;
            ViewBag.TransactionType = transactionType;
            ViewBag.Search = search;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalAmount = items.Sum(t => t.Amount);
            ViewBag.TotalDue = items.Sum(t => t.DueAmount);
            ViewBag.TodayTotal = await _transactionLogService.GetTodayTotalAsync();
            ViewBag.MonthTotal = await _transactionLogService.GetMonthTotalAsync();

            return View(items);
        }
    }
}
