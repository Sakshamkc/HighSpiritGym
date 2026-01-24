using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Models.Boxing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        member.Price = model.Price;
        member.Remarks = model.Remarks;

        // ✅ Update photo ONLY if new one is uploaded
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
}
