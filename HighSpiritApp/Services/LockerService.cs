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

        public async Task<IEnumerable<Locker>> SearchAsync(string? searchTerm)
        {
            return await _lockerRepository.SearchAsync(searchTerm);
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
            // Check if locker number already exists
            if (await _lockerRepository.IsLockerNumberExistsAsync(locker.LockerNumber))
                throw new InvalidOperationException($"Locker number '{locker.LockerNumber}' already exists.");

            locker.CreatedAt = DateTime.Now;
            locker.Status = "Available";

            await _lockerRepository.AddAsync(locker);
            await _lockerRepository.SaveChangesAsync();
            return locker;
        }

        public async Task UpdateAsync(Locker locker)
        {
            var existing = await _lockerRepository.GetByIdAsync(locker.LockerID);
            if (existing == null)
                throw new KeyNotFoundException($"Locker with ID {locker.LockerID} not found.");

            // Check if locker number is being changed to an existing one
            if (existing.LockerNumber != locker.LockerNumber)
            {
                if (await _lockerRepository.IsLockerNumberExistsAsync(locker.LockerNumber, locker.LockerID))
                    throw new InvalidOperationException($"Locker number '{locker.LockerNumber}' already exists.");
            }

            existing.LockerNumber = locker.LockerNumber;
            existing.Size = locker.Size;
            existing.Status = locker.Status;
            existing.CustomerID = locker.CustomerID;
            existing.AssignedTo = locker.AssignedTo;
            existing.AssignedPhone = locker.AssignedPhone;
            existing.StartDate = locker.StartDate;
            existing.EndDate = locker.EndDate;
            existing.RentalMonths = locker.RentalMonths;
            existing.MonthlyRate = locker.MonthlyRate;
            existing.TotalAmount = locker.TotalAmount;
            existing.PaidAmount = locker.PaidAmount;
            existing.DueAmount = locker.DueAmount;
            existing.KeyNumber = locker.KeyNumber;
            existing.KeyDeposit = locker.KeyDeposit;
            existing.KeyDepositAmount = locker.KeyDepositAmount;
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

        public async Task AssignLockerAsync(int lockerId, string memberName, string? phone, int? customerId, int months, decimal monthlyRate, decimal paidAmount)
        {
            var locker = await _lockerRepository.GetByIdAsync(lockerId);
            if (locker == null)
                throw new KeyNotFoundException($"Locker with ID {lockerId} not found.");

            if (locker.Status == "Occupied")
                throw new InvalidOperationException("Locker is already occupied.");

            var total = monthlyRate * months;

            locker.Status = "Occupied";
            locker.AssignedTo = memberName;
            locker.AssignedPhone = phone;
            locker.CustomerID = customerId;
            locker.StartDate = DateTime.Today;
            locker.EndDate = DateTime.Today.AddMonths(months);
            locker.RentalMonths = months;
            locker.MonthlyRate = monthlyRate;
            locker.TotalAmount = total;
            locker.PaidAmount = paidAmount;
            locker.DueAmount = total - paidAmount;
            locker.UpdatedAt = DateTime.Now;

            _lockerRepository.Update(locker);
            await _lockerRepository.SaveChangesAsync();
        }

        public async Task ReleaseLockerAsync(int lockerId)
        {
            var locker = await _lockerRepository.GetByIdAsync(lockerId);
            if (locker == null)
                throw new KeyNotFoundException($"Locker with ID {lockerId} not found.");

            locker.Status = "Available";
            locker.AssignedTo = null;
            locker.AssignedPhone = null;
            locker.CustomerID = null;
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

        public async Task<LockerStats> GetStatsAsync()
        {
            var all = await _lockerRepository.GetAllAsync();
            var lockers = all.ToList();
            var today = DateTime.Today;

            return new LockerStats
            {
                TotalLockers = lockers.Count,
                AvailableLockers = lockers.Count(l => l.Status == "Available"),
                OccupiedLockers = lockers.Count(l => l.Status == "Occupied"),
                MaintenanceLockers = lockers.Count(l => l.Status == "Maintenance"),
                ExpiredLockers = lockers.Count(l => l.Status == "Occupied" && l.EndDate < today),
                ExpiringSoonLockers = lockers.Count(l => l.Status == "Occupied" && l.EndDate >= today && l.EndDate <= today.AddDays(7)),
                TotalDueAmount = lockers.Sum(l => l.DueAmount),
                TotalRevenue = lockers.Sum(l => l.PaidAmount)
            };
        }

        public async Task<bool> IsLockerNumberExistsAsync(string lockerNumber, int? excludeId = null)
        {
            return await _lockerRepository.IsLockerNumberExistsAsync(lockerNumber, excludeId);
        }
    }
}
