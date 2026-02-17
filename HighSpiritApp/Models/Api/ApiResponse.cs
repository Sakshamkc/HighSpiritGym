namespace HighSpiritApp.Models.Api
{
    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string? message = null) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

        public static ApiResponse<T> Fail(string message) => new()
        {
            Success = false,
            Message = message
        };
    }

    /// <summary>
    /// Non-generic version for responses without data
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static ApiResponse Ok(string? message = null) => new()
        {
            Success = true,
            Message = message
        };

        public static ApiResponse Fail(string message) => new()
        {
            Success = false,
            Message = message
        };
    }

    /// <summary>
    /// Paginated response for list endpoints
    /// </summary>
    public class PaginatedResponse<T>
    {
        public bool Success { get; set; } = true;
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
