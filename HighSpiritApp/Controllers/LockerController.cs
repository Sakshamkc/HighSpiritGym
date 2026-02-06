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

        public LockerController(ILockerService lockerService)
        {
            _lockerService = lockerService;
        }

        public async Task<IActionResult> Index(string search, string status = "", string size = "", int page = 1)
        {
            int pageSize = 12;

            var allLockers = string.IsNullOrEmpty(search)
                ? await _lockerService.GetAllAsync()
                : await _lockerService.SearchAsync(search);

            var lockersList = allLockers.ToList();

            // Store counts before filtering
            var stats = await _lockerService.GetStatsAsync();
            ViewBag.CountAll = stats.TotalLockers;
            ViewBag.CountAvailable = stats.AvailableLockers;
            ViewBag.CountOccupied = stats.OccupiedLockers;
            ViewBag.CountMaintenance = stats.MaintenanceLockers;
            ViewBag.CountExpired = stats.ExpiredLockers;
            ViewBag.CountExpiringSoon = stats.ExpiringSoonLockers;
            ViewBag.TotalDue = stats.TotalDueAmount;
            ViewBag.TotalRevenue = stats.TotalRevenue;

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                lockersList = status switch
                {
                    "available" => lockersList.Where(l => l.Status == "Available").ToList(),
                    "occupied" => lockersList.Where(l => l.Status == "Occupied").ToList(),
                    "maintenance" => lockersList.Where(l => l.Status == "Maintenance").ToList(),
                    "expired" => lockersList.Where(l => l.Status == "Occupied" && l.IsExpired).ToList(),
                    "expiring" => lockersList.Where(l => l.Status == "Occupied" && l.IsExpiringSoon).ToList(),
                    _ => lockersList
                };
            }

            // Apply size filter
            if (!string.IsNullOrEmpty(size))
            {
                lockersList = lockersList.Where(l => l.Size == size).ToList();
            }

            int total = lockersList.Count;

            var data = lockersList
                .OrderBy(l => l.LockerNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Size = size;

            return View(data);
        }

        public IActionResult Create()
        {
            return View(new Locker { MonthlyRate = 500 }); // Default monthly rate
        }

        [HttpPost]
        public async Task<IActionResult> Create(Locker model)
        {
            try
            {
                await _lockerService.CreateAsync(model);
                TempData["success"] = $"Locker {model.LockerNumber} created successfully!";
                return RedirectToAction("Index");
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
                // Recalculate due amount
                model.DueAmount = model.TotalAmount - model.PaidAmount;
                model.DueAmount = Math.Max(0, model.DueAmount);

                await _lockerService.UpdateAsync(model);
                TempData["success"] = $"Locker {model.LockerNumber} updated successfully!";
                return RedirectToAction("Index");
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

            await _lockerService.DeleteAsync(id);
            TempData["success"] = $"Locker {locker.LockerNumber} deleted successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Assign(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null) return NotFound();

            if (locker.Status == "Occupied")
            {
                TempData["error"] = "This locker is already occupied.";
                return RedirectToAction("Index");
            }

            return View(locker);
        }

        [HttpPost]
        public async Task<IActionResult> Assign(int id, string memberName, string? phone, int months, decimal paidAmount)
        {
            try
            {
                var locker = await _lockerService.GetByIdAsync(id);
                if (locker == null) return NotFound();

                await _lockerService.AssignLockerAsync(id, memberName, phone, null, months, locker.MonthlyRate, paidAmount);
                TempData["success"] = $"Locker {locker.LockerNumber} assigned to {memberName} successfully!";
                return RedirectToAction("Index");
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

                await _lockerService.ReleaseLockerAsync(id);
                TempData["success"] = $"Locker {locker.LockerNumber} released successfully!";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Renew(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null) return NotFound();

            if (locker.Status != "Occupied")
            {
                TempData["error"] = "Only occupied lockers can be renewed.";
                return RedirectToAction("Index");
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
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Bulk create lockers
        public IActionResult BulkCreate()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BulkCreate(string prefix, int startNumber, int count, string size, decimal monthlyRate)
        {
            int created = 0;
            int skipped = 0;

            for (int i = 0; i < count; i++)
            {
                var lockerNumber = $"{prefix}{(startNumber + i).ToString().PadLeft(3, '0')}";

                if (await _lockerService.IsLockerNumberExistsAsync(lockerNumber))
                {
                    skipped++;
                    continue;
                }

                var locker = new Locker
                {
                    LockerNumber = lockerNumber,
                    Size = size,
                    Status = "Available",
                    MonthlyRate = monthlyRate
                };

                await _lockerService.CreateAsync(locker);
                created++;
            }

            TempData["success"] = $"Created {created} lockers. Skipped {skipped} (already exist).";
            return RedirectToAction("Index");
        }
    }
}
