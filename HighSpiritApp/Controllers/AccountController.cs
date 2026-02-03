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
        public IActionResult Login()
        {
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

        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Login");
        }
    }
}
