using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
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
                var lastRow = sheet.LastRowUsed();
                if (lastRow == null) continue;

                int rowCount = lastRow.RowNumber();

                for (int r = 2; r <= rowCount; r++)
                {
                    var row = sheet.Row(r);

                    string fullName = row.Cell(1).GetString().Trim();
                    if (string.IsNullOrEmpty(fullName)) continue;

                    DateTime joinDate;
                    var joinDateText = row.Cell(2).GetString().Trim();

                    joinDateText = joinDateText
                        .Replace("st", "")
                        .Replace("nd", "")
                        .Replace("rd", "")
                        .Replace("th", "");

                    if (!DateTime.TryParse(joinDateText, out joinDate))
                    {
                        joinDate = DateTime.Today;
                    }

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

            var latestMembership = customer.Memberships
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
                MembershipID = latestMembership.MembershipID,
                PaidPrice = latestMembership.PaidPrice,
                PlanName = latestMembership.PlanName
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
            customer.Occupation = vm.Occupation;
            customer.Shift = vm.Shift;
            customer.Remarks = vm.Remarks;
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await photoFile.CopyToAsync(ms);
                customer.Photo = ms.ToArray();
            }
            if (vm.MembershipID != null)
            {
                var membership = await _context.CustomerMemberships
                    .FindAsync(vm.MembershipID);

                if (membership != null)
                {
                    membership.PlanName = vm.PlanName;
                    membership.PaidPrice = vm.PaidPrice ?? 0;
                }
            }
            await _context.SaveChangesAsync();
            TempData["success"] = "User Details updated successfully!";
            return RedirectToAction("Index", "Customers");

        }

        [HttpGet]
        public async Task<IActionResult> ExportAll()
        {
            var customers = await _context.Customers
                .Include(c => c.Memberships)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Gym Members");

            // HEADER
            ws.Cell(1, 1).Value = "SN";
            ws.Cell(1, 2).Value = "Full Name";
            ws.Cell(1, 3).Value = "Phone";
            ws.Cell(1, 4).Value = "Email";
            ws.Cell(1, 5).Value = "Address";
            ws.Cell(1, 6).Value = "Gender";
            ws.Cell(1, 7).Value = "Blood Group";
            ws.Cell(1, 8).Value = "Weight (KG)";
            ws.Cell(1, 9).Value = "Height";
            ws.Cell(1, 10).Value = "Occupation";
            ws.Cell(1, 11).Value = "Join Date";
            ws.Cell(1, 12).Value = "Date Of Birth";
            ws.Cell(1, 13).Value = "Shift";
            ws.Cell(1, 14).Value = "Remarks";

            ws.Cell(1, 15).Value = "Plan Name";
            ws.Cell(1, 16).Value = "Paid Price";
            ws.Cell(1, 17).Value = "Start Date";
            ws.Cell(1, 18).Value = "Duration (Months)";
            ws.Cell(1, 19).Value = "Expire Date";
            ws.Cell(1, 20).Value = "Due Days";

            int row = 2;
            int sn = 1;

            foreach (var c in customers)
            {
                var m = c.Memberships?
                    .OrderByDescending(x => x.StartDate)
                    .FirstOrDefault();

                ws.Cell(row, 1).Value = sn++;
                ws.Cell(row, 2).Value = c.FullName;
                ws.Cell(row, 3).Value = c.Phone;
                ws.Cell(row, 4).Value = c.Email;
                ws.Cell(row, 5).Value = c.Address;
                ws.Cell(row, 6).Value = c.Gender;
                ws.Cell(row, 7).Value = c.BloodGroup;
                ws.Cell(row, 8).Value = c.WeightKG;
                ws.Cell(row, 9).Value = c.Height;
                ws.Cell(row, 10).Value = c.Occupation;
                ws.Cell(row, 11).Value = c.JoinDate.ToString("dd MMM yyyy");
                ws.Cell(row, 12).Value = c.DateOfBirth == DateTime.MinValue
                                            ? ""
                                            : c.DateOfBirth.ToString("dd MMM yyyy");
                ws.Cell(row, 13).Value = c.Shift;
                ws.Cell(row, 14).Value = c.Remarks;

                ws.Cell(row, 15).Value = m?.PlanName;
                ws.Cell(row, 16).Value = m?.PaidPrice ?? 0;
                ws.Cell(row, 17).Value = m?.StartDate.ToString("dd MMM yyyy");
                ws.Cell(row, 18).Value = m?.Duration ?? 0;
                ws.Cell(row, 19).Value = m?.ExpireDate?.ToString("dd MMM yyyy");
                ws.Cell(row, 20).Value = m?.DueDaysComputed ?? 0;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Gym Members Backup.xlsx"
            );
        }

    }
}
