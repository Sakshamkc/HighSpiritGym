using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    /// <summary>
    /// Report controller - Revenue analytics and reporting
    /// </summary>
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<IActionResult> Index(int? year)
        {
            var selectedYear = year ?? DateTime.Today.Year;

            // Get all report data
            var revenueReport = await _reportService.GetRevenueReportAsync();
            var monthlyData = await _reportService.GetMonthlyRevenueAsync(selectedYear);
            var recentTransactions = await _reportService.GetRecentTransactionsAsync(15);
            var categoryWise = await _reportService.GetCategoryWiseRevenueAsync();

            ViewBag.RevenueReport = revenueReport;
            ViewBag.MonthlyData = monthlyData;
            ViewBag.RecentTransactions = recentTransactions;
            ViewBag.CategoryWise = categoryWise;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.AvailableYears = Enumerable.Range(2020, DateTime.Today.Year - 2020 + 1).Reverse().ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyChartData(int year)
        {
            var data = await _reportService.GetMonthlyRevenueAsync(year);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryData()
        {
            var data = await _reportService.GetCategoryWiseRevenueAsync();
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> FilterByDate(DateTime? startDate, DateTime? endDate)
        {
            var report = await _reportService.GetRevenueReportAsync(startDate, endDate);
            return Json(report);
        }
    }
}
