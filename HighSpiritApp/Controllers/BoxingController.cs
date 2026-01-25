using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Models.Boxing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

[Authorize]
public class BoxingController : Controller
{
    private readonly GymDbContext _context;

    public BoxingController(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string search, int page = 1)
    {
        int pageSize = 10;

        var query = _context.BoxingMembers.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.GuardianName.Contains(search));
        }

        int total = await query.CountAsync();

        var data = await query
            .OrderByDescending(x => x.JoinDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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

        // 🔥 AUTO CALCULATE PRICE
        model.Price = model.CashAmount + model.EsewaAmount;

        _context.BoxingMembers.Add(model);
        await _context.SaveChangesAsync();

        TempData["success"] = "Boxing member added successfully!";
        return RedirectToAction("Index");
    }


    public async Task<IActionResult> Edit(int id)
    {
        var member = await _context.BoxingMembers.FindAsync(id);
        if (member == null) return NotFound();

        return View(member);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(BoxingMember model, IFormFile photoFile)
    {
        var member = await _context.BoxingMembers.FindAsync(model.BoxingMemberID);
        if (member == null) return NotFound();

        // Update normal fields
        member.Name = model.Name;
        member.JoinDate = model.JoinDate;
        member.GuardianName = model.GuardianName;
        member.GuardianContact = model.GuardianContact;
        member.PerMonthClass = model.PerMonthClass;
        member.CashAmount = model.CashAmount;
        member.EsewaAmount = model.EsewaAmount;
        member.Price = model.CashAmount + model.EsewaAmount;
        member.DueAmount = model.DueAmount;
        member.Price = model.Price;
        member.Remarks = model.Remarks;


        if (photoFile != null && photoFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await photoFile.CopyToAsync(ms);
            member.Photo = ms.ToArray();
        }

        await _context.SaveChangesAsync();

        TempData["success"] = "Boxing member updated successfully!";
        return RedirectToAction("Index");
    }


    public async Task<IActionResult> Details(int id)
    {
        var member = await _context.BoxingMembers.FindAsync(id);
        if (member == null) return NotFound();

        return View(member);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var member = await _context.BoxingMembers.FindAsync(id);
        if (member != null)
        {
            _context.BoxingMembers.Remove(member);
            await _context.SaveChangesAsync();
        }

        TempData["success"] = "Boxing member removed!";
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

        int imported = 0;
        int skipped = 0;

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        foreach (var sheet in workbook.Worksheets)
        {
            var lastRow = sheet.LastRowUsed();
            if (lastRow == null) continue;

            int rowCount = lastRow.RowNumber();

            // Start from row 2 (skip header)
            for (int r = 2; r <= rowCount; r++)
            {
                var row = sheet.Row(r);

                string name = row.Cell(1).GetString().Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                DateTime joinDate;
                var joinDateText = row.Cell(2).GetString().Trim();

                // Handles: "5th Aug 2024", "05/08/2024", etc.
                if (!DateTime.TryParseExact(
                        joinDateText.Replace("st", "")
                                    .Replace("nd", "")
                                    .Replace("rd", "")
                                    .Replace("th", ""),
                        new[] { "d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy", "dd/MM/yyyy", "yyyy-MM-dd" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out joinDate))
                {
                    joinDate = DateTime.Today;
                }
                string guardianName = row.Cell(3).GetString().Trim();
                string guardianContact = row.Cell(4).GetString().Trim();

                bool exists = await _context.BoxingMembers.AnyAsync(b =>
                    b.Name == name &&
                    b.GuardianContact == guardianContact &&
                    b.JoinDate.HasValue &&
                    b.JoinDate.Value.Date == joinDate.Date);

                if (exists)
                {
                    skipped++;
                    continue;
                }

                string perMonthClass =
                    string.IsNullOrWhiteSpace(row.Cell(5).GetString())
                    ? "0+0+0+0"
                    : row.Cell(5).GetString();

                int cash =
                    row.Cell(6).GetValue<int?>() ?? 0;

                int esewa =
                    row.Cell(7).GetValue<int?>() ?? 0;

                int due =
                    row.Cell(8).GetValue<int?>() ?? 0;

                string remarks =
                    row.Cell(9).GetString();

                var member = new BoxingMember
                {
                    Name = name,
                    JoinDate = joinDate,
                    GuardianName = guardianName,
                    GuardianContact = guardianContact,
                    PerMonthClass = perMonthClass,
                    CashAmount = cash,
                    EsewaAmount = esewa,
                    DueAmount = due,
                    Remarks = remarks,

                    // 🔥 AUTO CALCULATED
                    Price = cash + esewa
                };

                _context.BoxingMembers.Add(member);
                imported++;
            }
        }

        await _context.SaveChangesAsync();

        TempData["success"] =
            $"Import completed. Imported: {imported}, Skipped duplicates: {skipped}";

        return RedirectToAction("Index");
    }

}
