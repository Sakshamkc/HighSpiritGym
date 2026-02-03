using HighSpiritApp.Models;

namespace HighSpiritApp.Services.Interfaces
{
    /// <summary>
    /// Customer business logic service
    /// </summary>
    public interface ICustomerService
    {
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByIdWithMembershipsAsync(int id);
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<CustomerListResult> GetFilteredCustomersAsync(CustomerFilterRequest filter);
        Task<Customer> CreateAsync(Customer customer, byte[]? photo);
        Task UpdateAsync(CustomerEditVM viewModel, byte[]? photo);
        Task DeleteAsync(int id);
        Task<ImportResult> ImportFromExcelAsync(Stream fileStream);
        Task<byte[]> ExportToExcelAsync(CustomerFilterRequest filter);
    }

    public class CustomerFilterRequest
    {
        public string? Search { get; set; }
        public string? Filter { get; set; } = "all";
        public string? Sort { get; set; }
        public int? Duration { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CustomerListResult
    {
        public IEnumerable<Customer> Customers { get; set; } = new List<Customer>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public DurationCounts DurationCounts { get; set; } = new();
    }

    public class DurationCounts
    {
        public int Count1M { get; set; }
        public int Count3M { get; set; }
        public int Count6M { get; set; }
        public int Count12M { get; set; }
        public int CountAll { get; set; }
    }

    public class ImportResult
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public List<string> SkippedUsers { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
