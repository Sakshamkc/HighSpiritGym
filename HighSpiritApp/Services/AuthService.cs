using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace HighSpiritApp.Services
{
    /// <summary>
    /// Authentication service implementation using ASP.NET Identity
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AuthService(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<AuthResult> LoginAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid username or password."
                };
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, password, isPersistent: true, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return new AuthResult
                {
                    Success = true,
                    Username = user.UserName
                };
            }

            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password."
            };
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            // This would typically check the current HttpContext
            // For now, return a simple check
            await Task.CompletedTask;
            return false;
        }
    }
}
