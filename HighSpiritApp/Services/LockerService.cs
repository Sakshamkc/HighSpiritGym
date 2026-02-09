using ClosedXML.Excel;
using HighSpiritApp.Models.Locker;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;

namespace HighSpiritApp.Services
{
    /// <summary>
    /// Locker business logic implementation
    /// </summary>
    public class LockerService : ILockerService
    {
        private readonly ILockerRepository _lockerRepository;

        public LockerService(ILockerRepository lockerRepository)
        {
            _lockerRepository = lockerRepository;
        }

        public async Task<Locker?> GetByIdAsync(int id)
        {
            return await _lockerRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Locker>> GetAllAsync()
        {
            return await _lockerRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Locker>> GetByGenderAsync(string gender)
        {
            return await _lockerRepository.GetLockersByGenderAsync(gender);
        }

        public async Task<IEnumerable<Locker>> SearchAsync(string? searchTerm, string? gender = null)
        {
            return await _lockerRepository.SearchAsync(searchTerm, gender);
        }

        public async Task<IEnumerable<Locker>> GetByStatusAsync(string status)
        {
            return await _lockerRepository.GetLockersByStatusAsync(status);
        }

        public async Task<IEnumerable<Locker>> GetExpiredLockersAsync()
        {
            return await _lockerRepository.GetExpiredLockersAsync();
        }

        public async Task<IEnumerable<Locker>> GetExpiringSoonLockersAsync(int days = 7)
        {
            return await _lockerRepository.GetExpiringSoonLockersAsync(days);
        }

        public async Task<Locker?> GetRandomEmptyLockerAsync(string gender)
        {
            var allLockers = await _lockerRepository.GetLockersByGenderAsync(gender);
            var emptyLockers = allLockers
                .Where(l => l.Status == "Empty" || (l.Status != "Occupied" && l.Status != "Locked" && string.IsNullOrEmpty(l.AssignedTo)))
                .ToList();

            if (emptyLockers.Count == 0)
                return null;

            // Get a random empty locker
            var random = new Random();
            return emptyLockers[random.Next(emptyLockers.Count)];
        }

        public async Task<Locker?> GetLockerByMemberNameAsync(string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName))
                return null;

            var allLockers = await _lockerRepository.GetAllAsync();
            return allLockers.FirstOrDefault(l => 
                l.Status == "Occupied" && 
                !string.IsNullOrEmpty(l.AssignedTo) && 
                l.AssignedTo.Equals(memberName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Locker> CreateAsync(Locker locker)
        {
            if (await _lockerRepository.IsLockerNumberExistsAsync(locker.LockerNumber, locker.Gender))
                throw new InvalidOperationException($"Locker number '{locker.LockerNumber}' already exists for {locker.Gender}.");

            locker.CreatedAt = DateTime.Now;
            if (string.IsNullOrEmpty(locker.Status))
                locker.Status = "Empty";

            await _lockerRepository.AddAsync(locker);
            await _lockerRepository.SaveChangesAsync();
            return locker;
        }

        public async Task UpdateAsync(Locker locker)
        {
            var existing = await _lockerRepository.GetByIdAsync(locker.LockerID);
            if (existing == null)
                throw new KeyNotFoundException($"Locker with ID {locker.LockerID} not found.");

            if (existing.LockerNumber != locker.LockerNumber || existing.Gender != locker.Gender)
            {
                if (await _lockerRepository.IsLockerNumberExistsAsync(locker.LockerNumber, locker.Gender, locker.LockerID))
                    throw new InvalidOperationException($"Locker number '{locker.LockerNumber}' already exists for {locker.Gender}.");
            }

            existing.LockerNumber = locker.LockerNumber;
            existing.Gender = locker.Gender;
            existing.Status = locker.Status;
            existing.CustomerID = locker.CustomerID;
            existing.AssignedTo = locker.AssignedTo;
            existing.AssignedPhone = locker.AssignedPhone;
            existing.Package = locker.Package;
            existing.StartDate = locker.StartDate;
            existing.EndDate = locker.EndDate;
            existing.RentalMonths = locker.RentalMonths;
            existing.MonthlyRate = locker.MonthlyRate;
            existing.TotalAmount = locker.TotalAmount;
            existing.PaidAmount = locker.PaidAmount;
            existing.DueAmount = locker.DueAmount;
            existing.Remarks = locker.Remarks;
            existing.UpdatedAt = DateTime.Now;

            _lockerRepository.Update(existing);
            await _lockerRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var locker = await _lockerRepository.GetByIdAsync(id);
            if (locker != null)
            {
                _lockerRepository.Remove(locker);
                await _lockerRepository.SaveChangesAsync();
            }
        }

        public async Task AssignLockerAsync(int lockerId, string memberName, string? phone, int? customerId, string? package, int months, decimal totalAmount, decimal paidAmount)
        {
            var locker = await _lockerRepository.GetByIdAsync(lockerId);
            if (locker == null)
                throw new KeyNotFoundException($"Locker with ID {lockerId} not found.");

            if (locker.Status == "Occupied" && !string.IsNullOrEmpty(locker.AssignedTo))
                throw new InvalidOperationException("Locker is already occupied.");

            locker.Status = "Occupied";
            locker.AssignedTo = memberName;
            locker.AssignedPhone = phone;
            locker.CustomerID = customerId;
            locker.Package = package;
            locker.StartDate = DateTime.Today;
            locker.EndDate = DateTime.Today.AddMonths(months);
            locker.RentalMonths = months;
            locker.TotalAmount = totalAmount;
            locker.PaidAmount = paidAmount;
            locker.DueAmount = totalAmount - paidAmount;
            locker.MonthlyRate = months > 0 ? totalAmount / months : 0;
            locker.UpdatedAt = DateTime.Now;

            _lockerRepository.Update(locker);
            await _lockerRepository.SaveChangesAsync();
        }

        public async Task ReleaseLockerAsync(int lockerId)
        {
            var locker = await _lockerRepository.GetByIdAsync(lockerId);
            if (locker == null)
                throw new KeyNotFoundException($"Locker with ID {lockerId} not found.");

            locker.Status = "Empty";
            locker.AssignedTo = null;
            locker.AssignedPhone = null;
            locker.CustomerID = null;
            locker.Package = null;
            locker.StartDate = null;
            locker.EndDate = null;
            locker.RentalMonths = 0;
            locker.TotalAmount = 0;
            locker.PaidAmount = 0;
            locker.DueAmount = 0;
            locker.UpdatedAt = DateTime.Now;

            _lockerRepository.Update(locker);
            await _lockerRepository.SaveChangesAsync();
        }

        public async Task RenewLockerAsync(int lockerId, int months, decimal paidAmount)
        {
            var locker = await _lockerRepository.GetByIdAsync(lockerId);
            if (locker == null)
                throw new KeyNotFoundException($"Locker with ID {lockerId} not found.");

            var additionalTotal = locker.MonthlyRate * months;
            var newEndDate = locker.EndDate.HasValue && locker.EndDate.Value >= DateTime.Today
                ? locker.EndDate.Value.AddMonths(months)
                : DateTime.Today.AddMonths(months);

            locker.EndDate = newEndDate;
            locker.RentalMonths += months;
            locker.TotalAmount += additionalTotal;
            locker.PaidAmount += paidAmount;
            locker.DueAmount = locker.TotalAmount - locker.PaidAmount;
            locker.UpdatedAt = DateTime.Now;

            _lockerRepository.Update(locker);
            await _lockerRepository.SaveChangesAsync();
        }

        public async Task<LockerStats> GetStatsAsync(string? gender = null)
        {
            var all = await _lockerRepository.GetAllAsync();
            var lockers = all.ToList();
            
            if (!string.IsNullOrEmpty(gender))
            {
                lockers = lockers.Where(l => l.Gender == gender).ToList();
            }
            
            var today = DateTime.Today;

            return new LockerStats
            {
                TotalLockers = lockers.Count,
                OccupiedLockers = lockers.Count(l => l.Status == "Occupied" && !string.IsNullOrEmpty(l.AssignedTo) && (!l.EndDate.HasValue || l.EndDate >= today)),
                EmptyLockers = lockers.Count(l => l.Status == "Empty" || (l.Status != "Occupied" && l.Status != "Locked" && string.IsNullOrEmpty(l.AssignedTo))),
                LockedLockers = lockers.Count(l => l.Status == "Locked"),
                ExpiredLockers = lockers.Count(l => l.Status == "Occupied" && l.EndDate.HasValue && l.EndDate < today),
                ExpiringSoonLockers = lockers.Count(l => l.Status == "Occupied" && l.EndDate >= today && l.EndDate <= today.AddDays(7)),
                TotalDueAmount = lockers.Sum(l => l.DueAmount),
                TotalRevenue = lockers.Sum(l => l.PaidAmount),
                GentsTotal = all.Count(l => l.Gender == "Gents"),
                LadiesTotal = all.Count(l => l.Gender == "Ladies")
            };
        }

        public async Task<bool> IsLockerNumberExistsAsync(string lockerNumber, string gender, int? excludeId = null)
        {
            return await _lockerRepository.IsLockerNumberExistsAsync(lockerNumber, gender, excludeId);
        }

        public async Task InitializeLockersAsync(string gender, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                var lockerNumber = i.ToString();
                
                if (await _lockerRepository.IsLockerNumberExistsAsync(lockerNumber, gender))
                    continue;

                var locker = new Locker
                {
                    LockerNumber = lockerNumber,
                    Gender = gender,
                    Status = "Empty",
                    MonthlyRate = 0,
                    CreatedAt = DateTime.Now
                };

                await _lockerRepository.AddAsync(locker);
            }
            
            await _lockerRepository.SaveChangesAsync();
        }

        public async Task<LockerImportResult> ImportFromExcelAsync(Stream fileStream, string gender)
        {
            var result = new LockerImportResult { Success = true };

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var sheet = workbook.Worksheets.First();
                
                int headerRow = 1;
                for (int r = 1; r <= 10; r++)
                {
                    var cellValue = sheet.Cell(r, 1).GetString().Trim();
                    if (cellValue.Contains("Locker", StringComparison.OrdinalIgnoreCase))
                    {
                        headerRow = r;
                        break;
                    }
                }

                var lastRow = sheet.LastRowUsed();
                if (lastRow == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Excel file is empty.";
                    return result;
                }

                for (int r = headerRow + 1; r <= lastRow.RowNumber(); r++)
                {
                    try
                    {
                        var row = sheet.Row(r);
                        
                        var lockerNoCell = row.Cell(1).GetString().Trim();
                        if (string.IsNullOrEmpty(lockerNoCell)) continue;
                        
                        var lockerNumber = lockerNoCell;
                        if (int.TryParse(lockerNoCell, out int lockerNum))
                        {
                            lockerNumber = lockerNum.ToString();
                        }

                        var nameValue = row.Cell(2).GetString().Trim();
                        var package = row.Cell(3).GetString().Trim();
                        
                        var durationText = row.Cell(4).GetString().Trim();
                        int duration = 0;
                        if (!string.IsNullOrEmpty(durationText))
                        {
                            var numberPart = new string(durationText.Where(char.IsDigit).ToArray());
                            int.TryParse(numberPart, out duration);
                        }

                        DateTime? joinedDate = null;
                        var joinedCell = row.Cell(5);
                        if (joinedCell.DataType == XLDataType.DateTime)
                            joinedDate = joinedCell.GetDateTime();
                        else
                        {
                            var joinedText = joinedCell.GetString().Trim()
                                .Replace("st", "").Replace("nd", "")
                                .Replace("rd", "").Replace("th", "")
                                .Replace("Sept", "Sep");
                            if (DateTime.TryParse(joinedText, out DateTime parsed))
                                joinedDate = parsed;
                        }

                        decimal amount = 0;
                        var amountCell = row.Cell(6);
                        if (amountCell.DataType == XLDataType.Number)
                            amount = (decimal)amountCell.GetDouble();
                        else
                            decimal.TryParse(amountCell.GetString().Replace(",", "").Trim(), out amount);

                        DateTime? expiryDate = null;
                        var expiryCell = row.Cell(7);
                        if (expiryCell.DataType == XLDataType.DateTime)
                            expiryDate = expiryCell.GetDateTime();
                        else
                        {
                            var expiryText = expiryCell.GetString().Trim()
                                .Replace("st", "").Replace("nd", "")
                                .Replace("rd", "").Replace("th", "")
                                .Replace("Sept", "Sep");
                            if (DateTime.TryParse(expiryText, out DateTime parsed))
                                expiryDate = parsed;
                        }

                        decimal dueAmount = 0;
                        var dueCell = row.Cell(8);
                        if (dueCell.DataType == XLDataType.Number)
                            dueAmount = (decimal)dueCell.GetDouble();
                        else
                            decimal.TryParse(dueCell.GetString().Replace(",", "").Trim(), out dueAmount);

                        string status = "Empty";
                        string? assignedName = null;
                        
                        var nameLower = nameValue.ToLower();
                        if (nameLower == "empty" || string.IsNullOrEmpty(nameValue))
                            status = "Empty";
                        else if (nameLower == "locked")
                            status = "Locked";
                        else if (nameLower == "occupied")
                        {
                            // "Occupied" text without a name - mark as occupied with placeholder
                            status = "Occupied";
                            assignedName = "(Reserved)";
                        }
                        else if (nameLower == "maintenance")
                            status = "Maintenance";
                        else
                        {
                            status = "Occupied";
                            assignedName = nameValue;
                        }

                        var existingLocker = await _lockerRepository.GetByLockerNumberAsync(lockerNumber, gender);
                        
                        if (existingLocker != null)
                        {
                            existingLocker.Status = status;
                            existingLocker.AssignedTo = assignedName;
                            existingLocker.Package = string.IsNullOrEmpty(package) ? null : package;
                            existingLocker.RentalMonths = duration;
                            existingLocker.StartDate = joinedDate;
                            existingLocker.TotalAmount = amount;
                            existingLocker.PaidAmount = amount - dueAmount;
                            existingLocker.EndDate = expiryDate;
                            existingLocker.DueAmount = dueAmount;
                            existingLocker.MonthlyRate = duration > 0 && amount > 0 ? amount / duration : 0;
                            existingLocker.UpdatedAt = DateTime.Now;
                            
                            _lockerRepository.Update(existingLocker);
                            result.Updated++;
                        }
                        else
                        {
                            var locker = new Locker
                            {
                                LockerNumber = lockerNumber,
                                Gender = gender,
                                Status = status,
                                AssignedTo = assignedName,
                                Package = string.IsNullOrEmpty(package) ? null : package,
                                RentalMonths = duration,
                                StartDate = joinedDate,
                                TotalAmount = amount,
                                PaidAmount = amount - dueAmount,
                                EndDate = expiryDate,
                                DueAmount = dueAmount,
                                MonthlyRate = duration > 0 && amount > 0 ? amount / duration : 0,
                                CreatedAt = DateTime.Now
                            };

                            await _lockerRepository.AddAsync(locker);
                            result.Imported++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Row {r}: {ex.Message}");
                        result.Skipped++;
                    }
                }

                await _lockerRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<byte[]> ExportToExcelAsync(string gender, string? status = null)
        {
            var lockers = (await _lockerRepository.GetLockersByGenderAsync(gender)).ToList();

            // Apply status filter if specified
            if (!string.IsNullOrEmpty(status))
            {
                var today = DateTime.Today;
                lockers = status switch
                {
                    "empty" => lockers.Where(l => l.Status == "Empty" || (l.Status != "Occupied" && l.Status != "Locked" && string.IsNullOrEmpty(l.AssignedTo))).ToList(),
                    "occupied" => lockers.Where(l => l.Status == "Occupied" && !string.IsNullOrEmpty(l.AssignedTo)).ToList(),
                    "locked" => lockers.Where(l => l.Status == "Locked").ToList(),
                    "expired" => lockers.Where(l => l.Status == "Occupied" && l.EndDate < today).ToList(),
                    _ => lockers
                };
            }

            // Sort by locker number
            lockers = lockers
                .OrderBy(l => int.TryParse(l.LockerNumber, out int num) ? num : int.MaxValue)
                .ThenBy(l => l.LockerNumber)
                .ToList();

            using var workbook = new XLWorkbook();
            var sheetName = $"{gender} Lockers";
            if (!string.IsNullOrEmpty(status))
            {
                sheetName += $" - {char.ToUpper(status[0]) + status.Substring(1)}";
            }

            var ws = workbook.Worksheets.Add(sheetName);

            // Headers
            var headers = new[] { "SN", "Locker No", "Status", "Assigned To", "Phone", "Package", 
                "Duration (Months)", "Start Date", "Expiry Date", "Total Amount", "Paid Amount", 
                "Due Amount", "Days Remaining", "Remarks" };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = gender == "Gents" 
                    ? XLColor.FromHtml("#3b82f6") 
                    : XLColor.FromHtml("#ec4899");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int row = 2, sn = 1;
            foreach (var l in lockers)
            {
                ws.Cell(row, 1).Value = sn++;
                ws.Cell(row, 2).Value = l.LockerNumber;
                ws.Cell(row, 3).Value = l.Status;
                ws.Cell(row, 4).Value = l.AssignedTo ?? "";
                ws.Cell(row, 5).Value = l.AssignedPhone ?? "";
                ws.Cell(row, 6).Value = l.Package ?? "";
                ws.Cell(row, 7).Value = l.RentalMonths;
                ws.Cell(row, 8).Value = l.StartDate?.ToString("dd MMM yyyy") ?? "";
                ws.Cell(row, 9).Value = l.EndDate?.ToString("dd MMM yyyy") ?? "";
                ws.Cell(row, 10).Value = l.TotalAmount;
                ws.Cell(row, 11).Value = l.PaidAmount;
                ws.Cell(row, 12).Value = l.DueAmount;
                ws.Cell(row, 13).Value = l.IsExpired ? "Expired" : (l.DaysRemaining > 0 ? l.DaysRemaining.ToString() : "-");
                ws.Cell(row, 14).Value = l.Remarks ?? "";

                // Color coding for status
                if (l.Status == "Occupied" && l.IsExpired)
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fee2e2"); // Red for expired
                }
                else if (l.Status == "Empty")
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#dcfce7"); // Green for empty
                }
                else if (l.Status == "Locked")
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f5f9"); // Gray for locked
                }

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
