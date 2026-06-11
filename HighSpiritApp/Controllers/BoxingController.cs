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
        private readonly IActivityLogService _activityLogService;

        public BoxingController(IBoxingService boxingService, IActivityLogService activityLogService)
        {
            _boxingService = boxingService;
            _activityLogService = activityLogService;
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

            // Apply 'updated' filter (recently updated in last 30 days)
            if (filter == "updated")
            {
                var thirtyDaysAgo = DateTime.Today.AddDays(-30);
                membersList = membersList
                    .Where(m => m.UpdatedAt.HasValue && m.UpdatedAt.Value >= thirtyDaysAgo)
                    .OrderByDescending(m => m.UpdatedAt)
                    .ToList();
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

            await _activityLogService.LogAsync("Created", "Boxing", model.BoxingMemberID, model.Name, $"Created {category} boxing member {model.Name}", User.Identity?.Name ?? "Admin");

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
            member.ExpireDate = model.ExpireDate;

            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                member.Photo = ms.ToArray();
            }

            await _boxingService.UpdateAsync(member);

            await _activityLogService.LogAsync("Updated", "Boxing", member.BoxingMemberID, member.Name, $"Updated boxing member {member.Name}", User.Identity?.Name ?? "Admin");

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

        // Serve boxing member photo as a separate cacheable image endpoint.
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Photo(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            if (member?.Photo == null || member.Photo.Length == 0)
            {
                return NotFound();
            }
            return File(member.Photo, "image/jpeg");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            var category = member?.Category ?? "Children";

            await _boxingService.DeleteAsync(id);
            await _activityLogService.LogAsync("Deleted", "Boxing", id, member?.Name ?? $"ID:{id}", $"Deleted boxing member {member?.Name ?? $"ID:{id}"}", User.Identity?.Name ?? "Admin");
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
                "Per Month Class", "Cash Amount", "eSewa Amount", "Due Amount", "Expire Date", "Remarks" };

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
                ws.Cell(row, 10).Value = b.ExpireDate?.ToString("dd MMM yyyy");
                ws.Cell(row, 11).Value = b.Remarks;
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

        [HttpGet]
        public async Task<IActionResult> ExportDiary(string category = "Children")
        {
            if (category != "Adult" && category != "Children" && category != "All")
                category = "Children";

            var members = category == "All"
                ? (await _boxingService.GetAllAsync()).OrderBy(m => m.Name).ToList()
                : (await _boxingService.GetByCategoryAsync(category)).OrderBy(m => m.Name).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Contact Diary");

            // Title
            var title = category == "All" ? "ALL BOXING MEMBERS" : $"{category.ToUpper()} BOXING MEMBERS";
            ws.Cell(1, 1).Value = $"HIGH SPIRIT GYM - {title} CONTACT DIARY";
            ws.Range("A1:D1").Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 35;

            // Subtitle
            ws.Cell(2, 1).Value = $"Generated on: {DateTime.Now:dd MMM yyyy, hh:mm tt}";
            ws.Range("A2:D2").Merge();
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(2).Height = 20;

            // Headers
            var headers = new[] { "SN", "Name", "Guardian Name", "Guardian Contact" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(4, i + 1).Value = headers[i];
                ws.Cell(4, i + 1).Style.Font.Bold = true;
                ws.Cell(4, i + 1).Style.Font.FontColor = XLColor.White;
                ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#374151");
                ws.Cell(4, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(4, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            ws.Row(4).Height = 25;

            int row = 5, sn = 1;
            foreach (var b in members)
            {
                var isEven = sn % 2 == 0;
                var bgColor = isEven ? XLColor.FromHtml("#f3f4f6") : XLColor.White;

                ws.Cell(row, 1).Value = sn++;
                ws.Cell(row, 2).Value = b.Name;
                ws.Cell(row, 3).Value = b.GuardianName ?? "-";
                ws.Cell(row, 4).Value = b.GuardianContact ?? "-";

                for (int col = 1; col <= 4; col++)
                {
                    ws.Cell(row, col).Style.Fill.BackgroundColor = bgColor;
                    ws.Cell(row, col).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    ws.Cell(row, col).Style.Border.BottomBorderColor = XLColor.LightGray;
                    ws.Cell(row, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Row(row).Height = 22;
                row++;
            }

            // Footer
            row++;
            ws.Cell(row, 1).Value = $"Total Members: {sn - 1}";
            ws.Range(row, 1, row, 4).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Column widths
            ws.Column(1).Width = 6;
            ws.Column(2).Width = 28;
            ws.Column(3).Width = 25;
            ws.Column(4).Width = 20;

            // Print settings
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.Margins.SetLeft(0.5);
            ws.PageSetup.Margins.SetRight(0.5);

            using var diaryStream = new MemoryStream();
            workbook.SaveAs(diaryStream);

            return File(
                diaryStream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{category} Boxing Contact Diary.xlsx"
            );
        }
    }
}
