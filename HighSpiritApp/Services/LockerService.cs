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
                OccupiedLockers = lockers.Count(l => l.Status == "Occupied" && !string.IsNullOrEmpty(l.AssignedTo)),
                EmptyLockers = lockers.Count(l => l.Status == "Empty" || (l.Status != "Occupied" && l.Status != "Locked" && string.IsNullOrEmpty(l.AssignedTo))),
                LockedLockers = lockers.Count(l => l.Status == "Locked"),
                ExpiredLockers = lockers.Count(l => l.Status == "Occupied" && l.EndDate < today),
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
                            status = "Occupied";
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
    }
}
