using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    /// <summary>
    /// Account controller - Authentication
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        public IActionResult Login(bool? expired)
        {
            // Show session expired message if redirected due to timeout
            if (expired == true)
            {
                TempData["SessionExpired"] = true;
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            var result = await _authService.LoginAsync(username, password);

            if (result.Success)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = result.ErrorMessage;
            return View();
        }

        public async Task<IActionResult> Logout(bool? expired)
        {
            await _authService.LogoutAsync();
            
            if (expired == true)
            {
                return RedirectToAction("Login", new { expired = true });
            }
            
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Extends the user session by refreshing the authentication cookie
        /// Called via AJAX when user clicks "Stay Logged In"
        /// </summary>
        [HttpPost]
        [Authorize]
        public IActionResult ExtendSession()
        {
            // Simply returning OK will refresh the sliding expiration cookie
            return Ok(new { success = true, message = "Session extended" });
        }
    }
}
