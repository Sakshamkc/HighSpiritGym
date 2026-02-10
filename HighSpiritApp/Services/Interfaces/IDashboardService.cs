namespace HighSpiritApp.Services.Interfaces
{
    /// <summary>
    /// Dashboard statistics service
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardStats> GetDashboardStatsAsync();
    }

    public class DashboardStats
    {
        // Revenue Summary
        public decimal TotalRevenue { get; set; }
        public decimal GymRevenue { get; set; }
        public decimal LockerRevenue { get; set; }
        public decimal BoxingRevenue { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal MonthlyRevenue { get; set; }

        // Gym Stats
        public int GymTotal { get; set; }
        public int GymActive { get; set; }
        public int GymExpired { get; set; }
        public int GymExpiringSoon { get; set; }
        public int GymJoinedThisMonth { get; set; }
        public int GymTotalDue { get; set; }

        // Locker Stats
        public int LockerGentsTotal { get; set; }
        public int LockerGentsOccupied { get; set; }
        public int LockerGentsEmpty { get; set; }
        public int LockerGentsExpired { get; set; }
        public int LockerLadiesTotal { get; set; }
        public int LockerLadiesOccupied { get; set; }
        public int LockerLadiesEmpty { get; set; }
        public int LockerLadiesExpired { get; set; }
        public decimal LockerTotalDue { get; set; }

        // Boxing Stats
        public int BoxingTotal { get; set; }
        public int BoxingPaid { get; set; }
        public int BoxingWithDue { get; set; }
        public decimal BoxingTotalDue { get; set; }
    }
}
