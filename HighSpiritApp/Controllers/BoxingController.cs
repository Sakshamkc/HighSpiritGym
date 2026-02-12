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

        public async Task<IActionResult> Index(string category = "Children", string search = "", string filter = "all", string paymentStatus = "", int page = 1)
        {
            // Validate category
            if (category != "Adult" && category != "Children" && category != "All")
                category = "Children";

            int pageSize = 10;

            IEnumerable<BoxingMember> allMembers;
            if (category == "All")
            {
                allMembers = string.IsNullOrEmpty(search)
                    ? await _boxingService.GetAllAsync()
                    : await _boxingService.SearchAsync(search);
            }
            else
            {
                allMembers = string.IsNullOrEmpty(search)
                    ? await _boxingService.GetByCategoryAsync(category)
                    : await _boxingService.SearchByCategoryAsync(search, category);
            }

            var membersList = allMembers.ToList();
            
            // Store counts before filtering
            ViewBag.CountAll = membersList.Count;
            ViewBag.CountWithDue = membersList.Count(m => m.DueAmount > 0);
            ViewBag.CountPaid = membersList.Count(m => m.DueAmount == 0);
            
            // Apply payment status filter
            if (!string.IsNullOrEmpty(paymentStatus))
            {
                membersList = paymentStatus switch
                {
                    "due" => membersList.Where(m => m.DueAmount > 0).ToList(),
                    "paid" => membersList.Where(m => m.DueAmount == 0).ToList(),
                    _ => membersList
                };
            }

            int total = membersList.Count;

            var data = membersList
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Filter = filter;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.Category = category;

            return View(data);
        }

        public IActionResult Create(string category = "Children")
        {
            if (category != "Adult" && category != "Children")
                category = "Children";

            ViewBag.Category = category;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(BoxingMember model, IFormFile photoFile, string category = "Children")
        {
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                model.Photo = ms.ToArray();
            }

            // Auto calculate price
            model.Price = model.CashAmount + model.EsewaAmount;
            model.Category = category;

            await _boxingService.CreateAsync(model);

            TempData["success"] = $"{category} boxing member added successfully!";
            return RedirectToAction("Index", new { category });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            if (member == null) return NotFound();

            ViewBag.Category = member.Category;
            return View(member);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BoxingMember model, IFormFile photoFile)
        {
            var member = await _boxingService.GetByIdAsync(model.BoxingMemberID);
            if (member == null) return NotFound();

            var category = member.Category; // preserve original category

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
            return RedirectToAction("Index", new { category });
        }

        public async Task<IActionResult> Details(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            if (member == null) return NotFound();

            ViewBag.Category = member.Category;
            return View(member);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            var category = member?.Category ?? "Children";

            await _boxingService.DeleteAsync(id);
            TempData["success"] = "Boxing member deleted successfully.";
            return RedirectToAction("Index", new { category });
        }

        public IActionResult Import(string category = "Children")
        {
            if (category != "Adult" && category != "Children")
                category = "Children";

            ViewBag.Category = category;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, string category = "Children")
        {
            if (file == null || file.Length == 0)
            {
                TempData["error"] = "Please select an Excel file.";
                ViewBag.Category = category;
                return View();
            }

            using var stream = file.OpenReadStream();
            var result = await _boxingService.ImportFromExcelAsync(stream, category);

            if (result.Success)
            {
                TempData["success"] = $"Import completed. Imported: {result.Imported}, Skipped: {result.Skipped}";
            }
            else
            {
                TempData["error"] = $"Import failed: {result.ErrorMessage}";
            }

            return RedirectToAction("Index", new { category });
        }

        [HttpGet]
        public async Task<IActionResult> ExportAll(string category = "Children")
        {
            if (category != "Adult" && category != "Children" && category != "All")
                category = "Children";

            var members = category == "All"
                ? (await _boxingService.GetAllAsync()).ToList()
                : (await _boxingService.GetByCategoryAsync(category)).ToList();

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
                $"{category} Boxing Members Backup.xlsx"
            );
        }
    }
}
