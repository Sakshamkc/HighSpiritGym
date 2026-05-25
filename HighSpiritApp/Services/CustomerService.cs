using ClosedXML.Excel;
using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Services
{
    /// <summary>
    /// Customer business logic implementation
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IMembershipRepository _membershipRepository;
        private readonly GymDbContext _db;

        public CustomerService(
            ICustomerRepository customerRepository,
            IMembershipRepository membershipRepository,
            GymDbContext db)
        {
            _customerRepository = customerRepository;
            _membershipRepository = membershipRepository;
            _db = db;
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _customerRepository.GetByIdAsync(id);
        }

        public async Task<Customer?> GetByIdWithMembershipsAsync(int id)
        {
            return await _customerRepository.GetByIdWithMembershipsAsync(id);
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _customerRepository.GetAllWithMembershipsAsync();
        }

        public async Task<CustomerListResult> GetFilteredCustomersAsync(CustomerFilterRequest filter)
        {
            var today = DateTime.Today;
            var thirtyDaysAgo = today.AddDays(-30);
            filter.Filter ??= "all";

            // Build base SQL-translatable filters (avoid loading entities until needed)
            var query = _db.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(c =>
                    c.FullName.Contains(filter.Search) ||
                    (c.Phone != null && c.Phone.Contains(filter.Search)));
            }
            if (!string.IsNullOrEmpty(filter.Shift))
                query = query.Where(c => c.Shift == filter.Shift);
            if (!string.IsNullOrEmpty(filter.Gender))
                query = query.Where(c => c.Gender == filter.Gender);

            // JoinDate filters can be applied in SQL
            if (filter.Filter == "new")
                query = query.Where(c => c.JoinDate >= thirtyDaysAgo);
            else if (filter.Filter == "thismonth")
                query = query.Where(c => c.JoinDate.Month == today.Month && c.JoinDate.Year == today.Year);
            else if (filter.Filter == "updated")
                query = query.Where(c => c.UpdatedAt != null && c.UpdatedAt >= thirtyDaysAgo);

            // Project WITHOUT the heavy Photo blob; include lightweight membership projection.
            // (Active/expired/soon/hold/paid/due all depend on "latest membership per customer"
            //  which MySQL provider translates badly as a subquery — filter those in memory.)
            var rows = await query
                .Select(c => new Customer
                {
                    CustomerID = c.CustomerID,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    Email = c.Email,
                    Address = c.Address,
                    Gender = c.Gender,
                    BloodGroup = c.BloodGroup,
                    WeightKG = c.WeightKG,
                    Height = c.Height,
                    Occupation = c.Occupation,
                    JoinDate = c.JoinDate,
                    DateOfBirth = c.DateOfBirth,
                    Remarks = c.Remarks,
                    Shift = c.Shift,
                    QrToken = c.QrToken,
                    MustChangePassword = c.MustChangePassword,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    // Photo intentionally NOT loaded (blob).
                    Memberships = c.Memberships
                        .OrderByDescending(m => m.StartDate)
                        .Select(m => new CustomerMembership
                        {
                            MembershipID = m.MembershipID,
                            CustomerID = m.CustomerID,
                            PlanName = m.PlanName,
                            StartDate = m.StartDate,
                            ExpireDate = m.ExpireDate,
                            Duration = m.Duration,
                            PaidPrice = m.PaidPrice,
                            DueAmount = m.DueAmount,
                            IsActive = m.IsActive,
                            IsOnHold = m.IsOnHold,
                            HoldStartDate = m.HoldStartDate,
                            TotalHoldDays = m.TotalHoldDays,
                            CreatedAt = m.CreatedAt,
                            UpdatedAt = m.UpdatedAt
                        })
                        .ToList()
                })
                .ToListAsync();

            // Single lightweight query for "has photo" flags (id only). Set sentinel so the
            // view's `c.Photo != null` check still works without loading actual bytes.
            var idsWithPhotos = await _db.Customers.AsNoTracking()
                .Where(c => c.Photo != null)
                .Select(c => c.CustomerID)
                .ToListAsync();
            var photoSet = new HashSet<int>(idsWithPhotos);
            var sentinelPhoto = new byte[] { 1 };
            foreach (var c in rows)
            {
                if (photoSet.Contains(c.CustomerID))
                    c.Photo = sentinelPhoto;
            }

            IEnumerable<Customer> allCustomers = rows;

            // In-memory filters that depend on "latest membership per customer"
            CustomerMembership? Latest(Customer c) => c.Memberships?.FirstOrDefault();

            allCustomers = filter.Filter switch
            {
                "active" => allCustomers.Where(c =>
                {
                    var m = Latest(c);
                    return m != null && (m.ExpireDate >= today || m.IsOnHold);
                }),
                "expired" => allCustomers.Where(c =>
                {
                    var m = Latest(c);
                    return m != null && m.ExpireDate < today && !m.IsOnHold;
                }),
                "soon" => allCustomers.Where(c =>
                {
                    var m = Latest(c);
                    return m != null && m.ExpireDate >= today && m.ExpireDate <= today.AddDays(7);
                }),
                "hold" => allCustomers.Where(c =>
                {
                    var m = Latest(c);
                    return m != null && m.IsOnHold;
                }),
                _ => allCustomers
            };

            // Payment status filter (latest membership)
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
            {
                allCustomers = filter.PaymentStatus switch
                {
                    "paid" => allCustomers.Where(c => Latest(c)?.DueAmount == 0),
                    "due" => allCustomers.Where(c => (Latest(c)?.DueAmount ?? 0) > 0),
                    _ => allCustomers
                };
            }

            // Materialize once for plan / duration / sort / paging
            var list = allCustomers.ToList();

            // Plan name filter
            if (!string.IsNullOrEmpty(filter.PlanName))
            {
                var planFilter = filter.PlanName.ToLower();
                list = planFilter switch
                {
                    "custom2" or "custom-2" => list.Where(c => IsCustomPlan(c, 2)).ToList(),
                    "custom3" or "custom-3" => list.Where(c => IsCustomPlan(c, 3)).ToList(),
                    "gym" => list.Where(c => IsExactPlan(c, "Gym")).ToList(),
                    "cardio" => list.Where(c => IsExactPlan(c, "Cardio")).ToList(),
                    "premium" => list.Where(c => IsPlanContains(c, "Premium")).ToList(),
                    "zumba" => list.Where(c => IsExactPlanMultiple(c, new[] { "Zumba", "Aerobics" })).ToList(),
                    "sauna" => list.Where(c => IsExactPlanMultiple(c, new[] { "Sauna", "Steam" })).ToList(),
                    _ => list.Where(c => IsPlanContains(c, filter.PlanName)).ToList()
                };
            }

            // Duration filter
            if (filter.Duration.HasValue)
            {
                list = list.Where(c => Latest(c)?.Duration == filter.Duration.Value).ToList();
            }

            // Duration counts
            var durationCounts = new DurationCounts
            {
                Count1M = list.Count(c => Latest(c)?.Duration == 1),
                Count3M = list.Count(c => Latest(c)?.Duration == 3),
                Count6M = list.Count(c => Latest(c)?.Duration == 6),
                Count12M = list.Count(c => Latest(c)?.Duration == 12),
                CountAll = list.Count
            };

            // Sorting
            list = filter.Sort switch
            {
                "name_desc" => list.OrderByDescending(c => c.FullName).ToList(),
                "expire" => list.OrderBy(c => Latest(c)?.ExpireDate).ToList(),
                "expire_desc" => list.OrderByDescending(c => Latest(c)?.ExpireDate).ToList(),
                "join" => list.OrderBy(c => c.JoinDate).ToList(),
                "join_desc" => list.OrderByDescending(c => c.JoinDate).ToList(),
                "update" => list.OrderBy(c => c.UpdatedAt).ToList(),
                "update_desc" => list.OrderByDescending(c => c.UpdatedAt).ToList(),
                _ => filter.Filter == "new"
                    ? list.OrderByDescending(c => c.JoinDate).ToList()
                    : filter.Filter == "thismonth"
                        ? list.OrderByDescending(c => c.JoinDate).ToList()
                        : filter.Filter == "updated"
                            ? list.OrderByDescending(c => c.UpdatedAt).ToList()
                            : list.OrderBy(c => c.FullName).ToList()
            };

            var total = list.Count;
            var customers = list
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new CustomerListResult
            {
                Customers = customers,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                CurrentPage = filter.Page,
                DurationCounts = durationCounts
            };
        }

        // Helper: Check if plan contains keyword but is NOT a customized package
        private bool IsExactPlan(Customer c, string planKeyword)
        {
            var planName = c.Memberships?.OrderByDescending(m => m.StartDate).FirstOrDefault()?.PlanName;
            if (string.IsNullOrEmpty(planName)) return false;

            bool containsKeyword = planName.Contains(planKeyword, StringComparison.OrdinalIgnoreCase);
            bool isCustomized = planName.Contains("Customized", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Custom", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Two", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Three", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("(2)", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("(3)", StringComparison.OrdinalIgnoreCase);

            return containsKeyword && !isCustomized;
        }

        // Helper: Check if plan contains any of the keywords but is NOT a customized package
        private bool IsExactPlanMultiple(Customer c, string[] keywords)
        {
            var planName = c.Memberships?.OrderByDescending(m => m.StartDate).FirstOrDefault()?.PlanName;
            if (string.IsNullOrEmpty(planName)) return false;

            bool containsKeyword = keywords.Any(k => planName.Contains(k, StringComparison.OrdinalIgnoreCase));
            bool isCustomized = planName.Contains("Customized", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Custom", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Two", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Three", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("(2)", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("(3)", StringComparison.OrdinalIgnoreCase);

            return containsKeyword && !isCustomized;
        }

        // Helper: Check if customer has customized package (2 or 3 facilities)
        private bool IsCustomPlan(Customer c, int count)
        {
            var planName = c.Memberships?.OrderByDescending(m => m.StartDate).FirstOrDefault()?.PlanName;
            if (string.IsNullOrEmpty(planName)) return false;

            return count switch
            {
                2 => planName.Contains("Two", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("(2)", StringComparison.OrdinalIgnoreCase),
                3 => planName.Contains("Three", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("(3)", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        // Helper: Simple contains check (for Premium, etc.)
        private bool IsPlanContains(Customer c, string keyword)
        {
            var planName = c.Memberships?.OrderByDescending(m => m.StartDate).FirstOrDefault()?.PlanName;
            if (string.IsNullOrEmpty(planName)) return false;
            return planName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<Customer> CreateAsync(Customer customer, byte[]? photo)
        {
            if (photo != null)
                customer.Photo = photo;

            customer.CreatedAt = DateTime.Now;
            await _customerRepository.AddAsync(customer);
            await _customerRepository.SaveChangesAsync();
            return customer;
        }

        public async Task UpdateAsync(CustomerEditVM vm, byte[]? photo)
        {
            var customer = await _customerRepository.GetByIdAsync(vm.CustomerID);
            if (customer == null)
                throw new KeyNotFoundException($"Customer with ID {vm.CustomerID} not found.");

            // Update customer properties
            customer.FullName = vm.FullName;
            customer.Phone = vm.Phone;
            customer.Email = vm.Email;
            customer.Address = vm.Address;
            customer.Gender = vm.Gender;
            customer.DateOfBirth = vm.DateOfBirth;
            customer.Height = vm.Height;
            customer.WeightKG = vm.WeightKG;
            customer.BloodGroup = vm.BloodGroup;
            customer.Occupation = vm.Occupation;
            customer.Shift = vm.Shift;
            customer.Remarks = vm.Remarks;

            if (photo != null)
                customer.Photo = photo;

            customer.UpdatedAt = DateTime.Now;
            _customerRepository.Update(customer);

            // Update membership if provided
            if (vm.MembershipID != null)
            {
                var membership = await _membershipRepository.GetByIdAsync(vm.MembershipID.Value);
                if (membership != null)
                {
                    membership.PlanName = vm.PlanName;
                    membership.PaidPrice = vm.PaidPrice ?? 0;
                    membership.DueAmount = vm.DueAmount ?? 0;
                    membership.Duration = vm.Duration > 0 ? vm.Duration : membership.Duration;
                    membership.StartDate = vm.StartDate;
                    membership.ExpireDate = vm.ExpireDate ?? vm.StartDate.AddMonths(vm.Duration > 0 ? vm.Duration : membership.Duration);
                    membership.UpdatedAt = DateTime.Now;
                    _membershipRepository.Update(membership);
                }
            }

            await _customerRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var memberships = await _membershipRepository.GetByCustomerIdAsync(id);
            _membershipRepository.RemoveRange(memberships);

            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer != null)
                _customerRepository.Remove(customer);

            await _customerRepository.SaveChangesAsync();
        }

        public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream)
        {
            var result = new ImportResult { Success = true };

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                foreach (var sheet in workbook.Worksheets)
                {
                    var lastRow = sheet.LastRowUsed();
                    if (lastRow == null) continue;

                    for (int r = 2; r <= lastRow.RowNumber(); r++)
                    {
                        var row = sheet.Row(r);
                        var fullName = row.Cell(1).GetString().Trim();
                        if (string.IsNullOrEmpty(fullName)) continue;

                        // Parse join date
                        var joinText = row.Cell(2).GetString().Trim()
                            .Replace("st", "").Replace("nd", "")
                            .Replace("rd", "").Replace("th", "")
                            .Replace("Sept", "Sep");

                        if (!DateTime.TryParse(joinText, out DateTime joinDate))
                            joinDate = DateTime.Today;

                        // Check if exists
                        if (await _customerRepository.ExistsAsync(fullName, joinDate))
                        {
                            result.Skipped++;
                            result.SkippedUsers.Add($"Row {r}: {fullName} (Join: {joinDate:dd MMM yyyy}) - Already exists");
                            continue;
                        }

                        // Parse other fields
                        var planName = row.Cell(3).GetString();
                        var durationText = row.Cell(4).GetString();
                        var numberPart = new string(durationText.Where(char.IsDigit).ToArray());
                        int.TryParse(numberPart, out int duration);
                        if (duration == 0) duration = 1;

                        DateTime? expireDate = null;
                        var expireCell = row.Cell(5);
                        if (expireCell.DataType == XLDataType.DateTime)
                            expireDate = expireCell.GetDateTime();
                        else
                        {
                            var expireText = expireCell.GetString().Trim()
                                .Replace("st", "").Replace("nd", "")
                                .Replace("rd", "").Replace("th", "")
                                .Replace("Sept", "Sep");
                            if (DateTime.TryParse(expireText, out DateTime parsed))
                                expireDate = parsed;
                        }

                        var shift = row.Cell(6).GetString();
                        var remarks = row.Cell(7).GetString();

                        // Create customer
                        var customer = new Customer
                        {
                            FullName = fullName,
                            JoinDate = joinDate,
                            DateOfBirth = null,
                            Phone = "N/A",
                            Gender = "Unknown",
                            Address = "Imported from Excel",
                            Height = "N/A",
                            BloodGroup = "N/A",
                            Shift = string.IsNullOrWhiteSpace(shift) ? "General" : shift,
                            Remarks = remarks,
                            CreatedAt = DateTime.Now
                        };

                        await _customerRepository.AddAsync(customer);
                        await _customerRepository.SaveChangesAsync();

                        // Create membership
                        var membership = new CustomerMembership
                        {
                            CustomerID = customer.CustomerID,
                            PlanName = planName,
                            StartDate = joinDate,
                            Duration = duration,
                            ExpireDate = expireDate ?? joinDate.AddMonths(duration),
                            PaidPrice = 0,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };

                        await _membershipRepository.AddAsync(membership);
                        await _membershipRepository.SaveChangesAsync();

                        result.Imported++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<byte[]> ExportToExcelAsync(CustomerFilterRequest filter)
        {
            var customers = (await GetFilteredCustomersAsync(new CustomerFilterRequest
            {
                Search = filter.Search,
                Filter = filter.Filter,
                Duration = filter.Duration,
                PlanName = filter.PlanName,
                Shift = filter.Shift,
                Gender = filter.Gender,
                PaymentStatus = filter.PaymentStatus,
                PageSize = int.MaxValue
            })).Customers.OrderBy(c => c.FullName);

            using var workbook = new XLWorkbook();
            var sheetName = filter.Duration.HasValue ? $"{filter.Duration}M Members" :
                           filter.Filter != "all" ? $"{char.ToUpper(filter.Filter![0]) + filter.Filter.Substring(1)} Members" :
                           "Gym Members";

            var ws = workbook.Worksheets.Add(sheetName);

            // Headers
            var headers = new[] { "SN", "Full Name", "Phone", "Email", "Address", "Gender",
                "Blood Group", "Weight (KG)", "Height", "Occupation", "Join Date",
                "Date Of Birth", "Shift", "Remarks", "Plan Name", "Paid Price",
                "Start Date", "Duration (Months)", "Expire Date", "Due Days" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            int row = 2, sn = 1;
            foreach (var c in customers)
            {
                var m = c.Memberships?.OrderByDescending(x => x.StartDate).FirstOrDefault();

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
                ws.Cell(row, 12).Value = c.DateOfBirth?.ToString("dd MMM yyyy") ?? "";
                ws.Cell(row, 13).Value = c.Shift;
                ws.Cell(row, 14).Value = c.Remarks;
                ws.Cell(row, 15).Value = m?.PlanName;
                ws.Cell(row, 16).Value = m?.PaidPrice ?? 0;
                ws.Cell(row, 17).Value = m?.StartDate.ToString("dd MMM yyyy");
                ws.Cell(row, 18).Value = m?.Duration ?? 0;
                ws.Cell(row, 19).Value = m?.ExpireDate.ToString("dd MMM yyyy");
                ws.Cell(row, 20).Value = m?.DueDaysComputed ?? 0;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportDiaryAsync(CustomerFilterRequest filter)
        {
            var customers = (await GetFilteredCustomersAsync(new CustomerFilterRequest
            {
                Search = filter.Search,
                Filter = filter.Filter,
                Duration = filter.Duration,
                PlanName = filter.PlanName,
                Shift = filter.Shift,
                Gender = filter.Gender,
                PaymentStatus = filter.PaymentStatus,
                PageSize = int.MaxValue
            })).Customers.OrderBy(c => c.FullName);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Contact Diary");

            // Title
            ws.Cell(1, 1).Value = "HIGH SPIRIT GYM - CONTACT DIARY";
            ws.Range("A1:D1").Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 35;

            // Subtitle with date
            ws.Cell(2, 1).Value = $"Generated on: {DateTime.Now:dd MMM yyyy, hh:mm tt}";
            ws.Range("A2:D2").Merge();
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(2).Height = 20;

            // Headers
            var headers = new[] { "SN", "Full Name", "Address", "Phone", "Email" };
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
            foreach (var c in customers)
            {
                var isEven = sn % 2 == 0;
                var bgColor = isEven ? XLColor.FromHtml("#f3f4f6") : XLColor.White;

                ws.Cell(row, 1).Value = sn++;
                ws.Cell(row, 2).Value = c.FullName;
                ws.Cell(row, 3).Value = c.Address ?? "-";
                ws.Cell(row, 4).Value = c.Phone ?? "-";
                ws.Cell(row, 5).Value = c.Email ?? "-";

                for (int col = 1; col <= 5; col++)
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
            ws.Range(row, 1, row, 5).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Column widths
            ws.Column(1).Width = 6;   // SN
            ws.Column(2).Width = 28;  // Name
            ws.Column(3).Width = 25;  // Address
            ws.Column(4).Width = 18;  // Phone
            ws.Column(5).Width = 28;  // Email

            // Print settings
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.Margins.SetLeft(0.5);
            ws.PageSetup.Margins.SetRight(0.5);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
