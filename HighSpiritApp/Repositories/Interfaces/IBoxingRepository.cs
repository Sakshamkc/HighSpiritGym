using HighSpiritApp.Models.Boxing;

namespace HighSpiritApp.Repositories.Interfaces
{
    /// <summary>
    /// Boxing member-specific repository operations
    /// </summary>
    public interface IBoxingRepository : IRepository<BoxingMember>
    {
        Task<IEnumerable<BoxingMember>> GetMembersWithDueAsync();
        Task<IEnumerable<BoxingMember>> GetFullyPaidMembersAsync();
        Task<decimal> GetTotalDueAmountAsync();
        Task<IEnumerable<BoxingMember>> SearchAsync(string? searchTerm);
        Task<IEnumerable<BoxingMember>> GetByCategoryAsync(string category);
        Task<IEnumerable<BoxingMember>> SearchByCategoryAsync(string? searchTerm, string category);
        Task<IEnumerable<BoxingMember>> GetMembersWithDueByCategoryAsync(string category);
    }
}
