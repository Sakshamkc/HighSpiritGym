using HighSpiritApp.DataContext;
using HighSpiritApp.Models.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HighSpiritApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly GymDbContext _gymContext;

        public AuthController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            GymDbContext gymContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _gymContext = gymContext;
        }

        /// <summary>
        /// POST api/auth/login
        /// Authenticate and receive a JWT token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(ApiResponse.Fail("Username and password are required."));

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
                return Unauthorized(ApiResponse.Fail("Invalid username or password."));

            var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!validPassword)
                return Unauthorized(ApiResponse.Fail("Invalid username or password."));

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Customer";

            // Try to find linked customer by matching username/email with customer phone/email
            int? customerId = null;
            if (role == "Customer")
            {
                var customer = await _gymContext.Customers
                    .FirstOrDefaultAsync(c => c.Phone == request.Username
                        || c.Email == request.Username
                        || c.Phone == user.PhoneNumber);
                customerId = customer?.CustomerID;
            }

            // Check if customer must change password
            bool mustChangePassword = false;
            if (role == "Customer" && customerId.HasValue)
            {
                var customer = await _gymContext.Customers.FindAsync(customerId.Value);
                if (customer != null)
                    mustChangePassword = customer.MustChangePassword;
            }

            var token = GenerateJwtToken(user, role, customerId);
            var expiresAt = DateTime.UtcNow.AddDays(
                _configuration.GetValue<int>("Jwt:ExpireDays", 30));

            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                Token = token,
                Username = user.UserName!,
                Role = role,
                CustomerId = customerId,
                ExpiresAt = expiresAt,
                MustChangePassword = mustChangePassword
            }, "Login successful."));
        }

        /// <summary>
        /// POST api/auth/register
        /// Register a new customer account (linked to existing customer record)
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(ApiResponse.Fail("Username and password are required."));

            // Check if customer exists
            var customer = await _gymContext.Customers.FindAsync(request.CustomerID);
            if (customer == null)
                return NotFound(ApiResponse.Fail("Customer record not found. Contact gym admin."));

            // Check if user already exists
            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
                return BadRequest(ApiResponse.Fail("Username already taken."));

            var user = new IdentityUser
            {
                UserName = request.Username,
                Email = customer.Email,
                PhoneNumber = customer.Phone
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(ApiResponse.Fail(string.Join(", ", result.Errors.Select(e => e.Description))));

            // Ensure Customer role exists
            if (!await _roleManager.RoleExistsAsync("Customer"))
                await _roleManager.CreateAsync(new IdentityRole("Customer"));

            await _userManager.AddToRoleAsync(user, "Customer");

            // Mark customer as needing password change on first login
            customer.MustChangePassword = true;
            await _gymContext.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Registration successful. You can now login."));
        }

        /// <summary>
        /// POST api/auth/change-password
        /// Change password (used after first login)
        /// </summary>
        [HttpPost("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(ApiResponse.Fail("Current password and new password are required."));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Not authenticated."));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ApiResponse.Fail("User not found."));

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return BadRequest(ApiResponse.Fail(string.Join(", ", result.Errors.Select(e => e.Description))));

            // Clear mustChangePassword flag on the linked customer
            var customerIdClaim = User.FindFirstValue("CustomerId");
            if (!string.IsNullOrEmpty(customerIdClaim) && int.TryParse(customerIdClaim, out var custId))
            {
                var customer = await _gymContext.Customers.FindAsync(custId);
                if (customer != null)
                {
                    customer.MustChangePassword = false;
                    await _gymContext.SaveChangesAsync();
                }
            }

            return Ok(ApiResponse.Ok("Password changed successfully."));
        }

        /// <summary>
        /// GET api/auth/me
        /// Get current user info (requires authentication)
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(ApiResponse.Fail("Not authenticated."));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ApiResponse.Fail("User not found."));

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Customer";

            int? customerId = null;
            var customerIdClaim = User.FindFirstValue("CustomerId");
            if (!string.IsNullOrEmpty(customerIdClaim))
                customerId = int.Parse(customerIdClaim);

            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                Role = role,
                CustomerId = customerId
            }));
        }

        private string GenerateJwtToken(IdentityUser user, string role, int? customerId)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "HighSpiritGym_SuperSecret_Key_2026_!@#$%";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (customerId.HasValue)
                claims.Add(new Claim("CustomerId", customerId.Value.ToString()));

            var expireDays = _configuration.GetValue<int>("Jwt:ExpireDays", 30);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "HighSpiritApp",
                audience: _configuration["Jwt:Audience"] ?? "HighSpiritMobileApp",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expireDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int CustomerID { get; set; }
    }
}
