namespace HighSpiritApp.Services.Interfaces
{
    /// <summary>
    /// Report generation service for revenue analytics
    /// </summary>
    public interface IReportService
    {
        Task<RevenueReport> GetRevenueReportAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<MonthlyRevenueData> GetMonthlyRevenueAsync(int year);
        Task<List<RecentTransaction>> GetRecentTransactionsAsync(int count = 20);
        Task<CategoryWiseRevenue> GetCategoryWiseRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
    }

    public class RevenueReport
    {
        // Total Revenue
        public decimal TotalRevenue { get; set; }
        public decimal TotalDue { get; set; }
        public decimal TotalCollected { get; set; }

        // Category-wise Revenue
        public decimal GymRevenue { get; set; }
        public decimal GymDue { get; set; }
        public decimal BoxingRevenue { get; set; }
        public decimal BoxingDue { get; set; }
        public decimal LockerRevenue { get; set; }
        public decimal LockerDue { get; set; }

        // Counts
        public int TotalGymMembers { get; set; }
        public int TotalBoxingMembers { get; set; }
        public int TotalLockers { get; set; }
        public int ActiveLockers { get; set; }

        // This Period Stats
        public decimal ThisMonthRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public decimal RevenueGrowth { get; set; } // Percentage

        // Today's Stats
        public decimal TodayRevenue { get; set; }
        public int TodayTransactions { get; set; }
    }

    public class MonthlyRevenueData
    {
        public int Year { get; set; }
        public List<MonthRevenue> Months { get; set; } = new();
        public decimal YearTotal { get; set; }
    }

    public class MonthRevenue
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = "";
        public decimal GymRevenue { get; set; }
        public decimal BoxingRevenue { get; set; }
        public decimal LockerRevenue { get; set; }
        public decimal Total => GymRevenue + BoxingRevenue + LockerRevenue;
    }

    public class RecentTransaction
    {
        public int Id { get; set; }
        public string Type { get; set; } = ""; // Gym, Boxing, Locker
        public string MemberName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = ""; // Paid, Partial, Due
    }

    public class CategoryWiseRevenue
    {
        public decimal GymPercentage { get; set; }
        public decimal BoxingPercentage { get; set; }
        public decimal LockerPercentage { get; set; }
        public decimal GymAmount { get; set; }
        public decimal BoxingAmount { get; set; }
        public decimal LockerAmount { get; set; }
        public decimal Total { get; set; }
    }
}
