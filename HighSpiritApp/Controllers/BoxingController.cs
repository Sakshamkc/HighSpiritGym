using ClosedXML.Excel;
using HighSpiritApp.Models.Boxing;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers
{
    /// <summary>
    /// Boxing controller - Boxing member management
    /// </summary>
    [Authorize]
    public class BoxingController : Controller
    {
        private readonly IBoxingService _boxingService;

        public BoxingController(IBoxingService boxingService)
        {
            _boxingService = boxingService;
        }

        public async Task<IActionResult> Index(string search, int page = 1)
        {
            int pageSize = 10;

            var allMembers = string.IsNullOrEmpty(search)
                ? await _boxingService.GetAllAsync()
                : await _boxingService.SearchAsync(search);

            var membersList = allMembers.ToList();
            int total = membersList.Count;

            var data = membersList
                .OrderByDescending(x => x.JoinDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Search = search;

            return View(data);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(BoxingMember model, IFormFile photoFile)
        {
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                model.Photo = ms.ToArray();
            }

            // Auto calculate price
            model.Price = model.CashAmount + model.EsewaAmount;

            await _boxingService.CreateAsync(model);

            TempData["success"] = "Boxing member added successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            if (member == null) return NotFound();

            return View(member);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BoxingMember model, IFormFile photoFile)
        {
            var member = await _boxingService.GetByIdAsync(model.BoxingMemberID);
            if (member == null) return NotFound();

            // Update fields
            member.Name = model.Name;
            member.JoinDate = model.JoinDate;
            member.GuardianName = model.GuardianName;
            member.GuardianContact = model.GuardianContact;
            member.PerMonthClass = model.PerMonthClass;
            member.CashAmount = model.CashAmount;
            member.EsewaAmount = model.EsewaAmount;
            member.Price = model.CashAmount + model.EsewaAmount;
            member.DueAmount = model.DueAmount;
            member.Remarks = model.Remarks;

            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                member.Photo = ms.ToArray();
            }

            await _boxingService.UpdateAsync(member);

            TempData["success"] = "Boxing member updated successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            if (member == null) return NotFound();

            return View(member);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _boxingService.DeleteAsync(id);
            TempData["success"] = "Boxing member deleted successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult Import()
        {
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
            var result = await _boxingService.ImportFromExcelAsync(stream);

            if (result.Success)
            {
                TempData["success"] = $"Import completed. Imported: {result.Imported}, Skipped: {result.Skipped}";
            }
            else
            {
                TempData["error"] = $"Import failed: {result.ErrorMessage}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ExportAll()
        {
            var members = (await _boxingService.GetAllAsync()).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BoxingMembers");

            // Headers
            var headers = new[] { "SN", "Name", "Join Date", "Guardian Name", "Guardian Contact",
                "Per Month Class", "Cash Amount", "eSewa Amount", "Due Amount", "Remarks" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            int row = 2, sn = 1;
            foreach (var b in members)
            {
                ws.Cell(row, 1).Value = sn++;
                ws.Cell(row, 2).Value = b.Name;
                ws.Cell(row, 3).Value = b.JoinDate?.ToString("dd MMM yyyy");
                ws.Cell(row, 4).Value = b.GuardianName;
                ws.Cell(row, 5).Value = b.GuardianContact;
                ws.Cell(row, 6).Value = b.PerMonthClass;
                ws.Cell(row, 7).Value = b.CashAmount;
                ws.Cell(row, 8).Value = b.EsewaAmount;
                ws.Cell(row, 9).Value = b.DueAmount;
                ws.Cell(row, 10).Value = b.Remarks;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Boxing Members Backup.xlsx"
            );
        }
    }
}
