using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly GymDbContext _db;

        public ActivityLogService(GymDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string action, string entityType, int entityId, string entityName, string? description, string performedBy)
        {
            var log = new ActivityLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                PerformedBy = performedBy,
                PerformedAt = DateTime.Now
            };
            _db.ActivityLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ActivityLog>> GetRecentAsync(int count = 50)
        {
            return await _db.ActivityLogs.AsNoTracking()
                .OrderByDescending(a => a.PerformedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<(List<ActivityLog> Items, int TotalCount)> GetFilteredAsync(
            string? entityType, string? action, string? search, int page, int pageSize)
        {
            var query = _db.ActivityLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.EntityName.Contains(search) || (a.Description != null && a.Description.Contains(search)));

            var totalCount = await query.CountAsync();

            var items = await query.OrderByDescending(a => a.PerformedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
