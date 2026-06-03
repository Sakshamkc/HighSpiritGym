using HighSpiritApp.Models;

namespace HighSpiritApp.Services.Interfaces
{
    public interface IActivityLogService
    {
        Task LogAsync(string action, string entityType, int entityId, string entityName, string? description, string performedBy);
        Task<List<ActivityLog>> GetRecentAsync(int count = 50);
        Task<(List<ActivityLog> Items, int TotalCount)> GetFilteredAsync(string? entityType, string? action, string? search, int page, int pageSize);
    }
}
