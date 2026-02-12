using HighSpiritApp.Models.Boxing;
using HighSpiritApp.Services.Interfaces;

namespace HighSpiritApp.Services.Interfaces
{
    /// <summary>
    /// Boxing member business logic service
    /// </summary>
    public interface IBoxingService
    {
        Task<BoxingMember?> GetByIdAsync(int id);
        Task<IEnumerable<BoxingMember>> GetAllAsync();
        Task<IEnumerable<BoxingMember>> SearchAsync(string? searchTerm);
        Task<IEnumerable<BoxingMember>> GetMembersWithDueAsync();
        Task<BoxingMember> CreateAsync(BoxingMember member);
        Task UpdateAsync(BoxingMember member);
        Task DeleteAsync(int id);
        Task<ImportResult> ImportFromExcelAsync(Stream fileStream);
        Task<ImportResult> ImportFromExcelAsync(Stream fileStream, string category);
        Task<BoxingStats> GetStatsAsync();
        Task<IEnumerable<BoxingMember>> GetByCategoryAsync(string category);
        Task<IEnumerable<BoxingMember>> SearchByCategoryAsync(string? searchTerm, string category);
        Task<BoxingStats> GetStatsByCategoryAsync(string category);
    }

    public class BoxingStats
    {
        public int TotalMembers { get; set; }
        public int PaidMembers { get; set; }
        public int MembersWithDue { get; set; }
        public decimal TotalDueAmount { get; set; }
    }
}
