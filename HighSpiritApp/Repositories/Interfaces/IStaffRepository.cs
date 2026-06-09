using HighSpiritApp.Models;

namespace HighSpiritApp.Repositories.Interfaces
{
    public interface IStaffRepository : IRepository<Staff>
    {
        Task<IEnumerable<Staff>> SearchAsync(string? searchTerm);
        Task<IEnumerable<Staff>> GetActiveStaffAsync();
        Task<Staff?> GetByQrTokenAsync(string qrToken);
    }
}
