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

        public LockerController(ILockerService lockerService, ICustomerService customerService)
        {
            _lockerService = lockerService;
            _customerService = customerService;
        }

        public async Task<IActionResult> Index(string search, string gender = "Gents", string status = "", int page = 1)
        {
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
                    "occupied" => lockersList.Where(l => l.Status == "Occupied" && !string.IsNullOrEmpty(l.AssignedTo)).ToList(),
                    "locked" => lockersList.Where(l => l.Status == "Locked").ToList(),
                    "expired" => lockersList.Where(l => l.Status == "Occupied" && l.IsExpired).ToList(),
                    _ => lockersList
                };
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
            return View(locker);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null) return NotFound();

            var gender = locker.Gender;
            await _lockerService.DeleteAsync(id);
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
    }
}
