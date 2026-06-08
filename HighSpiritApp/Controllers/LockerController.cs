using HighSpiritApp.Models.Locker;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    /// <summary>
    /// Locker controller - Locker management
    /// </summary>
    [Authorize]
    public class LockerController : Controller
    {
        private readonly ILockerService _lockerService;
        private readonly ICustomerService _customerService;
        private readonly IActivityLogService _activityLogService;

        public LockerController(ILockerService lockerService, ICustomerService customerService, IActivityLogService activityLogService)
        {
            _lockerService = lockerService;
            _customerService = customerService;
            _activityLogService = activityLogService;
        }

        public async Task<IActionResult> Index(string search, string gender = "Gents", string status = "", string filter = "", int page = 1)
        {
            // Restrict 'updated' filter to SuperAdmin
            if (filter == "updated" && !User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", new { search, gender, status, page });

            int pageSize = 25;
            if (string.IsNullOrEmpty(gender)) gender = "Gents";

            var allLockers = await _lockerService.SearchAsync(search, gender);
            var lockersList = allLockers.ToList();

            var stats = await _lockerService.GetStatsAsync(gender);
            ViewBag.CountAll = stats.TotalLockers;
            ViewBag.CountOccupied = stats.OccupiedLockers;
            ViewBag.CountEmpty = stats.EmptyLockers;
            ViewBag.CountLocked = stats.LockedLockers;
            ViewBag.CountExpired = stats.ExpiredLockers;
            ViewBag.TotalDue = stats.TotalDueAmount;
            ViewBag.GentsTotal = stats.GentsTotal;
            ViewBag.LadiesTotal = stats.LadiesTotal;

            if (!string.IsNullOrEmpty(status))
            {
                lockersList = status switch
                {
                    "empty" => lockersList.Where(l => l.Status == "Empty" || (l.Status != "Occupied" && l.Status != "Locked" && string.IsNullOrEmpty(l.AssignedTo))).ToList(),
                    "occupied" => lockersList.Where(l => l.Status == "Occupied" && !string.IsNullOrEmpty(l.AssignedTo) && !l.IsExpired).ToList(),
                    "locked" => lockersList.Where(l => l.Status == "Locked").ToList(),
                    "expired" => lockersList.Where(l => l.Status == "Occupied" && l.IsExpired).ToList(),
                    _ => lockersList
                };
            }

            // Apply 'updated' filter (recently updated in last 30 days)
            if (filter == "updated")
            {
                var thirtyDaysAgo = DateTime.Today.AddDays(-30);
                lockersList = lockersList
                    .Where(l => l.UpdatedAt.HasValue && l.UpdatedAt.Value >= thirtyDaysAgo)
                    .OrderByDescending(l => l.UpdatedAt)
                    .ToList();
            }

            lockersList = lockersList
                .OrderBy(l => int.TryParse(l.LockerNumber, out int num) ? num : int.MaxValue)
                .ThenBy(l => l.LockerNumber)
                .ToList();

            int total = lockersList.Count;
            var data = lockersList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Gender = gender;
            ViewBag.Status = status;
            ViewBag.Filter = filter;

            return View(data);
        }

        public IActionResult Create(string gender = "Gents")
        {
            return View(new Locker { Gender = gender, Status = "Empty" });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Locker model)
        {
            try
            {
                await _lockerService.CreateAsync(model);
                await _activityLogService.LogAsync("Created", "Locker", model.LockerID, $"Locker {model.LockerNumber}", $"Created locker {model.LockerNumber} ({model.Gender})", User.Identity?.Name ?? "Admin");
                TempData["success"] = $"Locker {model.LockerNumber} created successfully!";
                return RedirectToAction("Index", new { gender = model.Gender });
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null) return NotFound();
            return View(locker);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Locker model)
        {
            try
            {
                model.DueAmount = Math.Max(0, model.TotalAmount - model.PaidAmount);
                await _lockerService.UpdateAsync(model);
                await _activityLogService.LogAsync("Updated", "Locker", model.LockerID, $"Locker {model.LockerNumber}", $"Updated locker {model.LockerNumber} ({model.Gender})", User.Identity?.Name ?? "Admin");
                TempData["success"] = $"Locker {model.LockerNumber} updated successfully!";
                return RedirectToAction("Index", new { gender = model.Gender });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null) return NotFound();

            // If locker is occupied, try to find the customer details
            if (!string.IsNullOrEmpty(locker.AssignedTo))
            {
                var filterRequest = new CustomerFilterRequest { Search = locker.AssignedTo, PageSize = 1 };
                var result = await _customerService.GetFilteredCustomersAsync(filterRequest);
                var customer = result.Customers.FirstOrDefault(c =>
                    c.FullName.Equals(locker.AssignedTo, StringComparison.OrdinalIgnoreCase));

                if (customer != null)
                {
                    // Get customer with memberships
                    var customerWithMemberships = await _customerService.GetByIdWithMembershipsAsync(customer.CustomerID);
                    ViewBag.AssignedCustomer = customerWithMemberships;
                }
            }

            return View(locker);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null) return NotFound();

            var gender = locker.Gender;
            await _lockerService.DeleteAsync(id);
            await _activityLogService.LogAsync("Deleted", "Locker", id, $"Locker {locker.LockerNumber}", $"Deleted locker {locker.LockerNumber} ({locker.Gender})", User.Identity?.Name ?? "Admin");
            TempData["success"] = $"Locker {locker.LockerNumber} deleted successfully.";
            return RedirectToAction("Index", new { gender });
        }

        public async Task<IActionResult> Assign(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null) return NotFound();

            if (locker.Status == "Occupied" && !string.IsNullOrEmpty(locker.AssignedTo))
            {
                TempData["error"] = "This locker is already assigned.";
                return RedirectToAction("Index", new { gender = locker.Gender });
            }

            return View(locker);
        }

        [HttpPost]
        public async Task<IActionResult> Assign(int id, string memberName, string? phone, string? package, int months, decimal totalAmount, decimal paidAmount)
        {
            try
            {
                var locker = await _lockerService.GetByIdAsync(id);
                if (locker == null) return NotFound();

                await _lockerService.AssignLockerAsync(id, memberName, phone, null, package, months, totalAmount, paidAmount);
                TempData["success"] = $"Locker {locker.LockerNumber} assigned to {memberName}!";
                return RedirectToAction("Index", new { gender = locker.Gender });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Release(int id)
        {
            try
            {
                var locker = await _lockerService.GetByIdAsync(id);
                if (locker == null) return NotFound();

                var gender = locker.Gender;
                await _lockerService.ReleaseLockerAsync(id);
                TempData["success"] = $"Locker {locker.LockerNumber} released!";
                return RedirectToAction("Index", new { gender });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Renew(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null) return NotFound();

            if (locker.Status != "Occupied")
            {
                TempData["error"] = "Only occupied lockers can be renewed.";
                return RedirectToAction("Index", new { gender = locker.Gender });
            }

            return View(locker);
        }

        [HttpPost]
        public async Task<IActionResult> Renew(int id, int months, decimal paidAmount)
        {
            try
            {
                var locker = await _lockerService.GetByIdAsync(id);
                if (locker == null) return NotFound();

                await _lockerService.RenewLockerAsync(id, months, paidAmount);
                TempData["success"] = $"Locker {locker.LockerNumber} renewed for {months} month(s)!";
                return RedirectToAction("Index", new { gender = locker.Gender });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        public IActionResult Import(string gender = "Gents")
        {
            ViewBag.Gender = gender;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, string gender)
        {
            if (file == null || file.Length == 0)
            {
                TempData["error"] = "Please select an Excel file.";
                return View();
            }

            using var stream = file.OpenReadStream();
            var result = await _lockerService.ImportFromExcelAsync(stream, gender);

            if (result.Success)
            {
                TempData["success"] = $"Import completed! Imported: {result.Imported}, Updated: {result.Updated}, Skipped: {result.Skipped}";
            }
            else
            {
                TempData["error"] = $"Import failed: {result.ErrorMessage}";
            }

            return RedirectToAction("Index", new { gender });
        }

        [HttpPost]
        public async Task<IActionResult> InitializeLockers(string gender, int count)
        {
            await _lockerService.InitializeLockersAsync(gender, count);
            TempData["success"] = $"Initialized {count} lockers for {gender}!";
            return RedirectToAction("Index", new { gender });
        }

        /// <summary>
        /// Export lockers to Excel based on gender and optional status filter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Export(string gender = "Gents", string? status = null)
        {
            var fileBytes = await _lockerService.ExportToExcelAsync(gender, status);

            var fileName = $"{gender} Lockers";
            if (!string.IsNullOrEmpty(status))
            {
                fileName += $" - {char.ToUpper(status[0]) + status.Substring(1)}";
            }
            fileName += ".xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        /// <summary>
        /// API endpoint to search gym members - returns full package name
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchMembers(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            var filterRequest = new CustomerFilterRequest { Search = term, PageSize = 20 };
            var result = await _customerService.GetFilteredCustomersAsync(filterRequest);

            var members = result.Customers.Select(m =>
            {
                var membership = m.Memberships?.OrderByDescending(x => x.StartDate).FirstOrDefault();
                return new
                {
                    id = m.CustomerID,
                    name = m.FullName,
                    phone = m.Phone ?? "",
                    package = membership?.PlanName ?? "", // Full package name like "Custom 2 - Gym & Cardio"
                    photo = m.Photo != null ? Convert.ToBase64String(m.Photo) : null
                };
            }).ToList();

            return Json(members);
        }

        /// <summary>
        /// Choose a locker to assign to a member from Customers page
        /// </summary>
        public async Task<IActionResult> ChooseLocker(int customerId)
        {
            // Get the customer details
            var customer = await _customerService.GetByIdWithMembershipsAsync(customerId);
            if (customer == null)
            {
                TempData["error"] = "Customer not found.";
                return RedirectToAction("Index", "Customers");
            }

            // Check if customer already has a locker assigned
            var existingLocker = await _lockerService.GetLockerByMemberNameAsync(customer.FullName);
            if (existingLocker != null)
            {
                TempData["error"] = $"{customer.FullName} already has Locker #{existingLocker.LockerNumber} assigned!";
                return RedirectToAction("Details", new { id = existingLocker.LockerID });
            }

            // Determine gender for locker (Male -> Gents, Female -> Ladies)
            var lockerGender = customer.Gender?.ToLower() == "female" ? "Ladies" : "Gents";

            // Get all empty lockers for this gender
            var allLockers = await _lockerService.GetByGenderAsync(lockerGender);
            var emptyLockers = allLockers
                .Where(l => l.Status == "Empty" || (l.Status != "Occupied" && l.Status != "Locked" && string.IsNullOrEmpty(l.AssignedTo)))
                .OrderBy(l => int.TryParse(l.LockerNumber, out int num) ? num : int.MaxValue)
                .ThenBy(l => l.LockerNumber)
                .ToList();

            if (!emptyLockers.Any())
            {
                TempData["error"] = $"No empty {lockerGender} lockers available!";
                return RedirectToAction("Index", new { gender = lockerGender });
            }

            // Get member's current package
            var membership = customer.Memberships?.OrderByDescending(x => x.StartDate).FirstOrDefault();
            var package = membership?.PlanName ?? "";

            // Pass data to the view
            ViewBag.Customer = customer;
            ViewBag.CustomerPackage = package;
            ViewBag.LockerGender = lockerGender;

            return View(emptyLockers);
        }

        /// <summary>
        /// Assign selected locker to customer (from ChooseLocker page)
        /// </summary>
        public async Task<IActionResult> AssignToCustomer(int lockerId, int customerId)
        {
            var locker = await _lockerService.GetByIdAsync(lockerId);
            if (locker == null) return NotFound();

            var customer = await _customerService.GetByIdWithMembershipsAsync(customerId);
            if (customer == null)
            {
                TempData["error"] = "Customer not found.";
                return RedirectToAction("Index", "Customers");
            }

            if (locker.Status == "Occupied" && !string.IsNullOrEmpty(locker.AssignedTo))
            {
                TempData["error"] = "This locker is already assigned.";
                return RedirectToAction("ChooseLocker", new { customerId });
            }

            // Get member's current package
            var membership = customer.Memberships?.OrderByDescending(x => x.StartDate).FirstOrDefault();
            var package = membership?.PlanName ?? "";

            // Pass data to the Assign view
            ViewBag.CustomerName = customer.FullName;
            ViewBag.CustomerPhone = customer.Phone;
            ViewBag.CustomerPackage = package;
            ViewBag.CustomerId = customerId;

            return View("Assign", locker);
        }
    }
}
