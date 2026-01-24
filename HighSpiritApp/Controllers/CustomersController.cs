using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly GymDbContext _context;
        public CustomersController(GymDbContext context)
        {
            _context = context;
        }

        // List customers with current membership
        public async Task<IActionResult> Index(string search, string sort, string filter, int page = 1)
        {
            int pageSize = 10;
            var today = DateTime.Today;

            var query = _context.Customers
                .Include(c => c.Memberships)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c =>
                    c.FullName.Contains(search) ||
                    c.Phone.Contains(search));
            }

            // Filters
            filter = filter ?? "all";
            ViewBag.Filter = filter;

            if (filter == "active")
            {
                query = query.Where(c =>
                    c.Memberships.Any(m => m.IsActive && m.DueDaysComputed == 0));
            }
            else if (filter == "expired")
            {
                query = query.Where(c =>
                    c.Memberships.Any(m => m.IsActive && m.DueDaysComputed > 0));
            }

            else if (filter == "soon")
            {
                query = query.Where(c =>
                    c.Memberships.Any(m =>
                        m.IsActive &&
                        m.ExpireDate >= today &&
                        m.ExpireDate <= today.AddDays(7)));
            }
            // Sorting
            ViewBag.Sort = sort;
            query = sort switch
            {
                "name_desc" => query.OrderByDescending(c => c.FullName),
                "expire" => query.OrderBy(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate)
                                 .Select(m => m.ExpireDate)
                                 .FirstOrDefault()),
                "expire_desc" => query.OrderByDescending(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate)
                                 .Select(m => m.ExpireDate)
                                 .FirstOrDefault()),
                _ => query.OrderBy(c => c.FullName),
            };

            int total = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Search = search;

            return View(data);
        }


        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Memberships)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
    Customer customer,
    IFormFile photoFile,
    string PlanName,
    int PaidPrice,
    DateTime StartDate,
    int Duration)
        {
            customer.JoinDate = DateTime.Now;

            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                customer.Photo = ms.ToArray();
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var membership = new CustomerMembership
            {
                CustomerID = customer.CustomerID,
                PlanName = PlanName,
                PaidPrice = PaidPrice,
                StartDate = StartDate,
                Duration = Duration,
                IsActive = true
            };

            _context.CustomerMemberships.Add(membership);
            await _context.SaveChangesAsync();

            TempData["success"] = "Customer added successfully!";
            return RedirectToAction("Index");
        }



        public async Task<IActionResult> Delete(int id)
        {
            var memberships = _context.CustomerMemberships
                                      .Where(m => m.CustomerID == id);

            _context.CustomerMemberships.RemoveRange(memberships);

            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Customer deleted successfully!";
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
                // 🔥 SAFE ROW DETECTION (WORKS FOR ALL SHEETS)
                var lastRow = sheet.LastRowUsed();
                if (lastRow == null) continue;

                int rowCount = lastRow.RowNumber();

                // Start from row 2 (skip header)
                for (int r = 2; r <= rowCount; r++)
                {
                    var row = sheet.Row(r);

                    string fullName = row.Cell(1).GetString().Trim();
                    if (string.IsNullOrEmpty(fullName)) continue;

                    DateTime joinDate =
                        row.Cell(2).GetValue<DateTime?>() ?? DateTime.Today;

                    // 🔍 FIXED DUPLICATE CHECK
                    bool exists = await _context.Customers.AnyAsync(c =>
                        c.FullName == fullName &&
                        c.JoinDate.Date == joinDate.Date);

                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    string planName = row.Cell(3).GetString();
                    int duration = row.Cell(4).GetValue<int?>() ?? 1;
                    string shift = row.Cell(6).GetString();
                    string remarks = row.Cell(7).GetString();

                    var customer = new Customer
                    {
                        FullName = fullName,
                        JoinDate = joinDate,
                        Phone = "N/A",
                        Email = null,
                        Gender = "Unknown",
                        Address = "Imported from Excel",
                        Height = "N/A",
                        WeightKG = null,
                        BloodGroup = "N/A",
                        Shift = string.IsNullOrWhiteSpace(shift) ? "General" : shift,
                        Remarks = remarks
                    };

                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();

                    var membership = new CustomerMembership
                    {
                        CustomerID = customer.CustomerID,
                        PlanName = planName,
                        Duration = duration,
                        StartDate = joinDate,
                        PaidPrice = 0,
                        IsActive = true
                    };

                    _context.CustomerMemberships.Add(membership);
                    await _context.SaveChangesAsync();

                    imported++;
                }
            }

            TempData["success"] =
                $"Import completed. Imported: {imported}, Skipped duplicates: {skipped}";

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> EditAll(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Memberships)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null) return NotFound();

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
                Shift = customer.Shift,
                Remarks = customer.Remarks
            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> EditAll(CustomerEditVM vm, IFormFile photoFile)
        {
            var customer = await _context.Customers.FindAsync(vm.CustomerID);
            if (customer == null) return NotFound();

            customer.FullName = vm.FullName;
            customer.Phone = vm.Phone;
            customer.Email = vm.Email;
            customer.Address = vm.Address;
            customer.Gender = vm.Gender;
            customer.DateOfBirth = (DateTime)vm.DateOfBirth;
            customer.Height = vm.Height;
            customer.WeightKG = vm.WeightKG;
            customer.BloodGroup = vm.BloodGroup;
            customer.Shift = vm.Shift;
            customer.Remarks = vm.Remarks;
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                customer.Photo = ms.ToArray();
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "User Details and Membership updated successfully!";
            return RedirectToAction("Index", "Customers");

        }

        int GetDurationFromSheetName(string sheetName)
        {
            if (sheetName.Contains("1")) return 1;
            if (sheetName.Contains("3")) return 3;
            if (sheetName.Contains("6")) return 6;
            if (sheetName.Contains("12")) return 12;
            return 1; // default
        }

    }
}
