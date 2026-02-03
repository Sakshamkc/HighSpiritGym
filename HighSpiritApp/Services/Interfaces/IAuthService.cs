namespace HighSpiritApp.Services.Interfaces
{
    /// <summary>
    /// Authentication service interface
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Username { get; set; }
    }
}
