using HighSpiritApp.Models;

namespace HighSpiritApp.Services.Interfaces
{
    public interface ITransactionLogService
    {
        Task LogAsync(string transactionType, string entityType, int entityId, string entityName,
            string? planName, decimal amount, decimal dueAmount, string? paymentMethod,
            string? description, string performedBy);
        Task<(List<TransactionLog> Items, int TotalCount)> GetFilteredAsync(
            string? entityType, string? transactionType, string? search,
            DateTime? from, DateTime? to, int page, int pageSize);
        Task<decimal> GetTodayTotalAsync();
        Task<decimal> GetMonthTotalAsync();
    }
}
