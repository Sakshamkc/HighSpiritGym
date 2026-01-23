using ClosedXML.Excel;
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
                return View();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var rows = sheet.RangeUsed().RowsUsed().Skip(1); // skip header

            foreach (var row in rows)
            {
                int paidPrice = 0;
                int duration = 0;
                decimal weight = 0;

                int.TryParse(row.Cell(8).GetString(), out paidPrice);
                int.TryParse(row.Cell(10).GetString(), out duration);
                decimal.TryParse(row.Cell(6).GetString(), out weight);

                DateTime startDate = DateTime.Now;
                DateTime.TryParse(row.Cell(9).GetString(), out startDate);

                var customer = new Customer
                {
                    FullName = row.Cell(1).GetString(),
                    Phone = row.Cell(2).GetString(),
                    Address = row.Cell(3).GetString(),
                    BloodGroup = row.Cell(4).GetString(),
                    Height = row.Cell(5).GetString(),
                    WeightKG = weight,
                    JoinDate = DateTime.Now
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                var membership = new CustomerMembership
                {
                    CustomerID = customer.CustomerID,
                    PlanName = row.Cell(7).GetString(),
                    PaidPrice = paidPrice,     // now int
                    StartDate = startDate,
                    Duration = duration,
                    IsActive = true
                };

                _context.CustomerMemberships.Add(membership);
                await _context.SaveChangesAsync();
            }

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


    }
}
