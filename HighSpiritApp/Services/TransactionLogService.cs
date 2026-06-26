using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Services
{
    public class TransactionLogService : ITransactionLogService
    {
        private readonly GymDbContext _db;

        public TransactionLogService(GymDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string transactionType, string entityType, int entityId, string entityName,
            string? planName, decimal amount, decimal dueAmount, string? paymentMethod,
            string? description, string performedBy)
        {
            var log = new TransactionLog
            {
                TransactionType = transactionType,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                PlanName = planName,
                Amount = amount,
                DueAmount = dueAmount,
                PaymentMethod = paymentMethod,
                Description = description,
                PerformedBy = performedBy,
                TransactionDate = DateTime.Now
            };
            _db.TransactionLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<(List<TransactionLog> Items, int TotalCount)> GetFilteredAsync(
            string? entityType, string? transactionType, string? search,
            DateTime? from, DateTime? to, int page, int pageSize)
        {
            var query = _db.TransactionLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(t => t.EntityType == entityType);
            if (!string.IsNullOrEmpty(transactionType))
                query = query.Where(t => t.TransactionType == transactionType);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.EntityName.Contains(search) || (t.Description != null && t.Description.Contains(search)));
            if (from.HasValue)
                query = query.Where(t => t.TransactionDate >= from.Value);
            if (to.HasValue)
                query = query.Where(t => t.TransactionDate < to.Value.AddDays(1));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<decimal> GetTodayTotalAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _db.TransactionLogs.AsNoTracking()
                .Where(t => t.TransactionDate >= today && t.TransactionDate < tomorrow)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        public async Task<decimal> GetMonthTotalAsync()
        {
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return await _db.TransactionLogs.AsNoTracking()
                .Where(t => t.TransactionDate >= monthStart)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }
    }
}
