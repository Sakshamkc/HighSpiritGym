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
        // Gym Stats
        public int GymTotal { get; set; }
        public int GymActive { get; set; }
        public int GymExpired { get; set; }
        public int GymExpiringSoon { get; set; }
        public int GymJoinedThisMonth { get; set; }
        public int GymTotalDue { get; set; }

        // Boxing Stats
        public int BoxingTotal { get; set; }
        public int BoxingPaid { get; set; }
        public int BoxingWithDue { get; set; }
        public decimal BoxingTotalDue { get; set; }
    }
}
