using HighSpiritApp.Models.Locker;

namespace HighSpiritApp.Services.Interfaces
{
    /// <summary>
    /// Locker business logic service
    /// </summary>
    public interface ILockerService
    {
        Task<Locker?> GetByIdAsync(int id);
        Task<IEnumerable<Locker>> GetAllAsync();
        Task<IEnumerable<Locker>> SearchAsync(string? searchTerm);
        Task<IEnumerable<Locker>> GetByStatusAsync(string status);
        Task<IEnumerable<Locker>> GetExpiredLockersAsync();
        Task<IEnumerable<Locker>> GetExpiringSoonLockersAsync(int days = 7);
        Task<Locker> CreateAsync(Locker locker);
        Task UpdateAsync(Locker locker);
        Task DeleteAsync(int id);
        Task AssignLockerAsync(int lockerId, string memberName, string? phone, int? customerId, int months, decimal monthlyRate, decimal paidAmount);
        Task ReleaseLockerAsync(int lockerId);
        Task RenewLockerAsync(int lockerId, int months, decimal paidAmount);
        Task<LockerStats> GetStatsAsync();
        Task<bool> IsLockerNumberExistsAsync(string lockerNumber, int? excludeId = null);
    }

    public class LockerStats
    {
        public int TotalLockers { get; set; }
        public int AvailableLockers { get; set; }
        public int OccupiedLockers { get; set; }
        public int MaintenanceLockers { get; set; }
        public int ExpiredLockers { get; set; }
        public int ExpiringSoonLockers { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
