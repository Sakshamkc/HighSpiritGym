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
        Task<IEnumerable<Locker>> GetByGenderAsync(string gender);
        Task<IEnumerable<Locker>> SearchAsync(string? searchTerm, string? gender = null);
        Task<IEnumerable<Locker>> GetByStatusAsync(string status);
        Task<IEnumerable<Locker>> GetExpiredLockersAsync();
        Task<IEnumerable<Locker>> GetExpiringSoonLockersAsync(int days = 7);
        Task<Locker> CreateAsync(Locker locker);
        Task UpdateAsync(Locker locker);
        Task DeleteAsync(int id);
        Task AssignLockerAsync(int lockerId, string memberName, string? phone, int? customerId, string? package, int months, decimal totalAmount, decimal paidAmount);
        Task ReleaseLockerAsync(int lockerId);
        Task RenewLockerAsync(int lockerId, int months, decimal paidAmount);
        Task<LockerStats> GetStatsAsync(string? gender = null);
        Task<bool> IsLockerNumberExistsAsync(string lockerNumber, string gender, int? excludeId = null);
        Task<LockerImportResult> ImportFromExcelAsync(Stream fileStream, string gender);
        Task InitializeLockersAsync(string gender, int count);
    }

    public class LockerStats
    {
        public int TotalLockers { get; set; }
        public int OccupiedLockers { get; set; }
        public int EmptyLockers { get; set; }
        public int LockedLockers { get; set; }
        public int ExpiredLockers { get; set; }
        public int ExpiringSoonLockers { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalRevenue { get; set; }
        public int GentsTotal { get; set; }
        public int LadiesTotal { get; set; }
    }

    public class LockerImportResult
    {
        public bool Success { get; set; }
        public int Imported { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
}
