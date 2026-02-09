using HighSpiritApp.Models;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    /// <summary>
    /// Customers controller - Gym member management
    /// </summary>
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IMembershipService _membershipService;
        private readonly ILockerService _lockerService;

        public CustomersController(
            ICustomerService customerService,
            IMembershipService membershipService,
            ILockerService lockerService)
        {
            _customerService = customerService;
            _membershipService = membershipService;
            _lockerService = lockerService;
        }

        public async Task<IActionResult> Index(string search, string sort, string filter, int? duration, string planName, string shift, string gender, string paymentStatus, int page = 1)
        {
            var result = await _customerService.GetFilteredCustomersAsync(new CustomerFilterRequest
            {
                Search = search,
                Sort = sort,
                Filter = filter,
                Duration = duration,
                PlanName = planName,
                Shift = shift,
                Gender = gender,
                PaymentStatus = paymentStatus,
                Page = page
            });

            // Get all occupied lockers to check which customers already have lockers
            // Use GroupBy to handle duplicate names (like "(reserved)") - take first locker for each name
            var allLockers = await _lockerService.GetAllAsync();
            var assignedLockers = allLockers
                .Where(l => l.Status == "Occupied" && !string.IsNullOrEmpty(l.AssignedTo))
                .GroupBy(l => l.AssignedTo!.ToLower())
                .ToDictionary(g => g.Key, g => g.First());
            
            ViewBag.AssignedLockers = assignedLockers;

            ViewBag.Page = result.CurrentPage;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.Filter = filter ?? "all";
            ViewBag.Duration = duration;
            ViewBag.PlanName = planName;
            ViewBag.Shift = shift;
            ViewBag.Gender = gender;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.Count1M = result.DurationCounts.Count1M;
            ViewBag.Count3M = result.DurationCounts.Count3M;
            ViewBag.Count6M = result.DurationCounts.Count6M;
            ViewBag.Count12M = result.DurationCounts.Count12M;
            ViewBag.CountAll = result.DurationCounts.CountAll;

            return View(result.Customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetByIdWithMembershipsAsync(id);
            if (customer == null)
                return NotFound();

            // Check if customer has a locker
            var locker = await _lockerService.GetLockerByMemberNameAsync(customer.FullName);
            ViewBag.AssignedLocker = locker;

            return View(customer);
        }

        public IActionResult Create(string? planName = null)
        {
            ViewBag.PreselectedPlan = planName;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            Customer customer,
            IFormFile photoFile,
            string PlanName,
            int PaidPrice,
            int DueAmount,
            DateTime StartDate,
            int Duration,
            DateTime? ExpireDate)
        {
            byte[]? photo = null;
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                photo = ms.ToArray();
            }

            var createdCustomer = await _customerService.CreateAsync(customer, photo);

            // Create initial membership
            var membership = new CustomerMembership
            {
                CustomerID = createdCustomer.CustomerID,
                PlanName = PlanName,
                PaidPrice = PaidPrice,
                DueAmount = DueAmount,
                StartDate = StartDate,
                Duration = Duration,
                ExpireDate = ExpireDate ?? StartDate.AddMonths(Duration),
                IsActive = true
            };

            if (membership.StartDate < customer.JoinDate)
            {
                ModelState.AddModelError("StartDate", "Start date cannot be earlier than join date.");
                return View(customer);
            }

            await _membershipService.CreateAsync(membership);

            TempData["success"] = "Customer added successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _customerService.DeleteAsync(id);
            TempData["success"] = "Customer deleted successfully!";
            return RedirectToAction("Index");
        }

        public IActionResult Import(string? planName = null)
        {
            ViewBag.PreselectedPlan = planName;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["error"] = "Please select an Excel file.";
                return View();
            }

            using var stream = file.OpenReadStream();
            var result = await _customerService.ImportFromExcelAsync(stream);

            if (result.Success)
            {
                TempData["success"] = $"Import completed. Imported: {result.Imported}, Skipped: {result.Skipped}";

                if (result.SkippedUsers.Any())
                {
                    TempData["skippedUsers"] = string.Join("||", result.SkippedUsers);
                }
            }
            else
            {
                TempData["error"] = $"Import failed: {result.ErrorMessage}";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> EditAll(int id)
        {
            var customer = await _customerService.GetByIdWithMembershipsAsync(id);
            if (customer == null) return NotFound();

            // Get current membership - get latest by StartDate (no IsActive filter)
            var latestMembership = customer.Memberships?
                .OrderByDescending(m => m.StartDate)
                .FirstOrDefault();

            var vm = new CustomerEditVM
            {
                CustomerID = customer.CustomerID,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                Gender = customer.Gender,
                DateOfBirth = customer.DateOfBirth,
                Height = customer.Height,
                WeightKG = customer.WeightKG,
                BloodGroup = customer.BloodGroup,
                Occupation = customer.Occupation,
                Shift = customer.Shift,
                Remarks = customer.Remarks,
                MembershipID = latestMembership?.MembershipID,
                PaidPrice = latestMembership?.PaidPrice,
                DueAmount = latestMembership?.DueAmount,
                PlanName = latestMembership?.PlanName,
                StartDate = latestMembership?.StartDate ?? DateTime.Today,
                Duration = latestMembership?.Duration ?? 1,
                ExpireDate = latestMembership?.ExpireDate
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> EditAll(CustomerEditVM vm, IFormFile photoFile)
        {
            byte[]? photo = null;
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                photo = ms.ToArray();
            }

            await _customerService.UpdateAsync(vm, photo);

            TempData["success"] = "User Details updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ExportAll(string search, string filter, int? duration, string planName, string shift, string gender, string paymentStatus)
        {
            var fileBytes = await _customerService.ExportToExcelAsync(new CustomerFilterRequest
            {
                Search = search,
                Filter = filter ?? "all",
                Duration = duration,
                PlanName = planName,
                Shift = shift,
                Gender = gender,
                PaymentStatus = paymentStatus
            });

            var fileName = duration.HasValue ? $"{duration}M Members" : "Gym Members";
            if (filter != null && filter != "all")
                fileName += $" - {char.ToUpper(filter[0]) + filter.Substring(1)}";
            if (!string.IsNullOrEmpty(planName))
                fileName += $" - {planName}";
            if (!string.IsNullOrEmpty(shift))
                fileName += $" - {shift}";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{fileName}.xlsx"
            );
        }
    }
}
