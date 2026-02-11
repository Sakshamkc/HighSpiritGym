using HighSpiritApp.Models.Attendance;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;

namespace HighSpiritApp.Services
{
    /// <summary>
    /// Attendance service for QR-based punch in/out logic
    /// </summary>
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ICustomerRepository _customerRepository;

        public AttendanceService(
            IAttendanceRepository attendanceRepository,
            ICustomerRepository customerRepository)
        {
            _attendanceRepository = attendanceRepository;
            _customerRepository = customerRepository;
        }

        public async Task<PunchResult> PunchAsync(int customerId)
        {
            // Look up customer by ID
            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null)
            {
                return new PunchResult
                {
                    Success = false,
                    Message = "No member found with this ID.",
                    PunchType = "Error"
                };
            }

            // Check today's record for this customer
            var todayRecord = await _attendanceRepository.GetTodayRecordAsync(customerId);

            if (todayRecord == null)
            {
                // First punch today → Punch In
                var record = new AttendanceRecord
                {
                    CustomerID = customerId,
                    CustomerName = customer.FullName,
                    PunchInTime = DateTime.Now,
                    Date = DateOnly.FromDateTime(DateTime.Today)
                };

                await _attendanceRepository.AddAsync(record);
                await _attendanceRepository.SaveChangesAsync();

                return new PunchResult
                {
                    Success = true,
                    Message = $"Welcome, {customer.FullName}! Punched In successfully.",
                    PunchType = "PunchIn",
                    CustomerName = customer.FullName,
                    Time = record.PunchInTime
                };
            }

            if (!todayRecord.PunchOutTime.HasValue)
            {
                // Already punched in, no punch out yet → Punch Out
                todayRecord.PunchOutTime = DateTime.Now;
                _attendanceRepository.Update(todayRecord);
                await _attendanceRepository.SaveChangesAsync();

                return new PunchResult
                {
                    Success = true,
                    Message = $"Goodbye, {customer.FullName}! Punched Out successfully.",
                    PunchType = "PunchOut",
                    CustomerName = customer.FullName,
                    Time = todayRecord.PunchOutTime
                };
            }

            // Already punched in and out today
            return new PunchResult
            {
                Success = false,
                Message = $"{customer.FullName}, you have already punched in and out today.",
                PunchType = "AlreadyDone",
                CustomerName = customer.FullName
            };
        }

        public async Task<IEnumerable<AttendanceRecord>> GetAttendanceByDateAsync(DateOnly date)
        {
            return await _attendanceRepository.GetByDateAsync(date);
        }

        public async Task<IEnumerable<AttendanceRecord>> GetAttendanceByRangeAsync(DateOnly from, DateOnly to)
        {
            return await _attendanceRepository.GetByDateRangeAsync(from, to);
        }

        public async Task<AttendanceSummary> GetTodaySummaryAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var todayRecords = (await _attendanceRepository.GetByDateAsync(today)).ToList();

            return new AttendanceSummary
            {
                TotalCheckIns = todayRecords.Count,
                CurrentlyInGym = todayRecords.Count(r => !r.PunchOutTime.HasValue),
                AlreadyLeft = todayRecords.Count(r => r.PunchOutTime.HasValue)
            };
        }
    }
}
