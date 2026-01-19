using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HighSpiritApp.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Temporary hardcoded user (you can move to DB later)
            if (username == "admin" && password == "1234")
            {
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Owner")
            };

                var identity = new ClaimsIdentity(claims, "GymAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("GymAuth", principal);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid username or password";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("GymAuth");
            return RedirectToAction("Login");
        }
    }
}
