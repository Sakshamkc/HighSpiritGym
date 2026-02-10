using ClosedXML.Excel;
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

        public CustomerService(
            ICustomerRepository customerRepository,
            IMembershipRepository membershipRepository)
        {
            _customerRepository = customerRepository;
            _membershipRepository = membershipRepository;
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
            var query = _customerRepository.GetQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(c =>
                    c.FullName.Contains(filter.Search) ||
                    (c.Phone != null && c.Phone.Contains(filter.Search)));
            }

            // Status filter - based on latest membership's ExpireDate
            filter.Filter ??= "all";
            query = filter.Filter switch
            {
                "active" => query.Where(c =>
                    c.Memberships.Any() &&
                    c.Memberships.OrderByDescending(m => m.StartDate).First().ExpireDate >= today),
                "expired" => query.Where(c =>
                    c.Memberships.Any() &&
                    c.Memberships.OrderByDescending(m => m.StartDate).First().ExpireDate < today),
                "soon" => query.Where(c =>
                    c.Memberships.Any() &&
                    c.Memberships.OrderByDescending(m => m.StartDate).First().ExpireDate >= today &&
                    c.Memberships.OrderByDescending(m => m.StartDate).First().ExpireDate <= today.AddDays(7)),
                _ => query
            };

            // Shift filter
            if (!string.IsNullOrEmpty(filter.Shift))
            {
                query = query.Where(c => c.Shift == filter.Shift);
            }

            // Gender filter
            if (!string.IsNullOrEmpty(filter.Gender))
            {
                query = query.Where(c => c.Gender == filter.Gender);
            }

            // Payment status filter
            if (!string.IsNullOrEmpty(filter.PaymentStatus))
            {
                query = filter.PaymentStatus switch
                {
                    "paid" => query.Where(c =>
                        c.Memberships.Any() &&
                        c.Memberships.OrderByDescending(m => m.StartDate).First().DueAmount == 0),
                    "due" => query.Where(c =>
                        c.Memberships.Any() &&
                        c.Memberships.OrderByDescending(m => m.StartDate).First().DueAmount > 0),
                    _ => query
                };
            }

            // Load all matching customers to memory for plan filtering
            var allCustomers = await query.ToListAsync();

            // Plan name filter - EXACT matching logic (in-memory)
            if (!string.IsNullOrEmpty(filter.PlanName))
            {
                var planFilter = filter.PlanName.ToLower();
                
                allCustomers = planFilter switch
                {
                    "custom2" or "custom-2" => allCustomers.Where(c => IsCustomPlan(c, 2)).ToList(),
                    "custom3" or "custom-3" => allCustomers.Where(c => IsCustomPlan(c, 3)).ToList(),
                    "gym" => allCustomers.Where(c => IsExactPlan(c, "Gym")).ToList(),
                    "cardio" => allCustomers.Where(c => IsExactPlan(c, "Cardio")).ToList(),
                    "premium" => allCustomers.Where(c => IsPlanContains(c, "Premium")).ToList(),
                    "zumba" => allCustomers.Where(c => IsExactPlanMultiple(c, new[] { "Zumba", "Aerobics" })).ToList(),
                    "sauna" => allCustomers.Where(c => IsExactPlanMultiple(c, new[] { "Sauna", "Steam" })).ToList(),
                    _ => allCustomers.Where(c => IsPlanContains(c, filter.PlanName)).ToList()
                };
            }

            // Duration filter
            if (filter.Duration.HasValue)
            {
                allCustomers = allCustomers.Where(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate).FirstOrDefault()?.Duration == filter.Duration.Value
                ).ToList();
            }

            // Calculate duration counts
            var durationCounts = new DurationCounts
            {
                Count1M = allCustomers.Count(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate).FirstOrDefault()?.Duration == 1),
                Count3M = allCustomers.Count(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate).FirstOrDefault()?.Duration == 3),
                Count6M = allCustomers.Count(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate).FirstOrDefault()?.Duration == 6),
                Count12M = allCustomers.Count(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate).FirstOrDefault()?.Duration == 12),
                CountAll = allCustomers.Count
            };

            // Sorting
            allCustomers = filter.Sort switch
            {
                "name_desc" => allCustomers.OrderByDescending(c => c.FullName).ToList(),
                "expire" => allCustomers.OrderBy(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate).FirstOrDefault()?.ExpireDate).ToList(),
                "expire_desc" => allCustomers.OrderByDescending(c =>
                    c.Memberships.OrderByDescending(m => m.StartDate).FirstOrDefault()?.ExpireDate).ToList(),
                _ => allCustomers.OrderBy(c => c.FullName).ToList()
            };

            var total = allCustomers.Count;
            var customers = allCustomers
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
    }
}
