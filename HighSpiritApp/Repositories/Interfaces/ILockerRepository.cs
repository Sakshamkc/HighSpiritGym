using HighSpiritApp.Models.Locker;

namespace HighSpiritApp.Repositories.Interfaces
{
    /// <summary>
    /// Locker-specific repository operations
    /// </summary>
    public interface ILockerRepository : IRepository<Locker>
    {
        Task<IEnumerable<Locker>> GetAvailableLockersAsync();
        Task<IEnumerable<Locker>> GetOccupiedLockersAsync();
        Task<IEnumerable<Locker>> GetExpiredLockersAsync();
        Task<IEnumerable<Locker>> GetExpiringSoonLockersAsync(int days = 7);
        Task<IEnumerable<Locker>> GetLockersByStatusAsync(string status);
        Task<IEnumerable<Locker>> GetLockersByGenderAsync(string gender);
        Task<IEnumerable<Locker>> SearchAsync(string? searchTerm, string? gender = null);
        Task<Locker?> GetByLockerNumberAsync(string lockerNumber, string gender);
        Task<bool> IsLockerNumberExistsAsync(string lockerNumber, string gender, int? excludeId = null);
        Task<decimal> GetTotalDueAmountAsync();
        Task<int> GetCountByGenderAsync(string gender);
    }
}
