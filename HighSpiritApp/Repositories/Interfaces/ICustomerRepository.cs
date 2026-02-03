using HighSpiritApp.Models;

namespace HighSpiritApp.Repositories.Interfaces
{
    /// <summary>
    /// Customer-specific repository operations
    /// </summary>
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetByIdWithMembershipsAsync(int id);
        Task<IEnumerable<Customer>> GetAllWithMembershipsAsync();
        Task<IEnumerable<Customer>> SearchAsync(string? searchTerm);
        Task<bool> ExistsAsync(string fullName, DateTime joinDate);
        IQueryable<Customer> GetQueryable();
    }
}
