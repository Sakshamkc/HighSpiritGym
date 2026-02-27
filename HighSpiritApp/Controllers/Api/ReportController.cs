using HighSpiritApp.Models.Api;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// GET api/report/revenue?startDate=2026-01-01&endDate=2026-01-31
        /// Get revenue report for a date range
        /// </summary>
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var report = await _reportService.GetRevenueReportAsync(startDate, endDate);
            return Ok(ApiResponse<RevenueReport>.Ok(report));
        }

        /// <summary>
        /// GET api/report/monthly?year=2026
        /// Get monthly revenue breakdown for a year
        /// </summary>
        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int? year)
        {
            var data = await _reportService.GetMonthlyRevenueAsync(year ?? DateTime.Now.Year);
            return Ok(ApiResponse<MonthlyRevenueData>.Ok(data));
        }

        /// <summary>
        /// GET api/report/transactions?count=20
        /// Get recent transactions
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetRecentTransactions([FromQuery] int count = 20)
        {
            var transactions = await _reportService.GetRecentTransactionsAsync(count);
            return Ok(ApiResponse<List<RecentTransaction>>.Ok(transactions));
        }

        /// <summary>
        /// GET api/report/category-revenue?startDate=2026-01-01&endDate=2026-01-31
        /// Get category-wise revenue breakdown
        /// </summary>
        [HttpGet("category-revenue")]
        public async Task<IActionResult> GetCategoryRevenue(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var data = await _reportService.GetCategoryWiseRevenueAsync(startDate, endDate);
            return Ok(ApiResponse<CategoryWiseRevenue>.Ok(data));
        }
    }
}
