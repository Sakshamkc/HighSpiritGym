namespace HighSpiritApp.Models.Api
{
    // ===================== AUTH =====================
    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Role { get; set; } = "Customer";
        public int? CustomerId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    // ===================== CUSTOMER =====================
    public class CustomerDto
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public string? BloodGroup { get; set; }
        public decimal? WeightKG { get; set; }
        public string? Height { get; set; }
        public string? Occupation { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhotoBase64 { get; set; }
        public string? Remarks { get; set; }
        public string? Shift { get; set; }
        public DateTime CreatedAt { get; set; }

        // Active membership info (flattened)
        public string? CurrentPlan { get; set; }
        public DateTime? MembershipStart { get; set; }
        public DateTime? MembershipExpire { get; set; }
        public int? PaidPrice { get; set; }
        public int? DueAmount { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public bool IsOnHold { get; set; }
    }

    public class CustomerCreateRequest
    {
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public string? BloodGroup { get; set; }
        public decimal? WeightKG { get; set; }
        public string? Height { get; set; }
        public string? Occupation { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhotoBase64 { get; set; }
        public string? Remarks { get; set; }
        public string? Shift { get; set; }

        // Initial membership
        public string? PlanName { get; set; }
        public int PaidPrice { get; set; }
        public int DueAmount { get; set; }
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
    }

    public class CustomerUpdateRequest
    {
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public string? BloodGroup { get; set; }
        public decimal? WeightKG { get; set; }
        public string? Height { get; set; }
        public string? Occupation { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhotoBase64 { get; set; }
        public string? Remarks { get; set; }
        public string? Shift { get; set; }

        // Membership update
        public int? MembershipID { get; set; }
        public string? PlanName { get; set; }
        public int? PaidPrice { get; set; }
        public int? DueAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpireDate { get; set; }
    }

    // ===================== MEMBERSHIP =====================
    public class MembershipDto
    {
        public int MembershipID { get; set; }
        public int CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public string? PlanName { get; set; }
        public int PaidPrice { get; set; }
        public int DueAmount { get; set; }
        public int TotalPrice { get; set; }
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
        public DateTime ExpireDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired => ExpireDate < DateTime.Today;
    }

    public class MembershipRenewRequest
    {
        public int CustomerID { get; set; }
        public string? PlanName { get; set; }
        public int PaidPrice { get; set; }
        public int DueAmount { get; set; }
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
    }

    public class MembershipUpdateRequest
    {
        public string? PlanName { get; set; }
        public int? PaidPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpireDate { get; set; }
    }

    // ===================== BOXING =====================
    public class BoxingMemberDto
    {
        public int BoxingMemberID { get; set; }
        public string Name { get; set; } = null!;
        public DateTime? JoinDate { get; set; }
        public string GuardianName { get; set; } = null!;
        public string GuardianContact { get; set; } = null!;
        public string PerMonthClass { get; set; } = "0+0+0+0";
        public int CashAmount { get; set; }
        public int EsewaAmount { get; set; }
        public int DueAmount { get; set; }
        public int Price { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? PhotoBase64 { get; set; }
        public string Category { get; set; } = "Children";
        public DateTime CreatedAt { get; set; }
    }

    public class BoxingMemberCreateRequest
    {
        public string Name { get; set; } = null!;
        public DateTime? JoinDate { get; set; }
        public string GuardianName { get; set; } = null!;
        public string GuardianContact { get; set; } = null!;
        public string PerMonthClass { get; set; } = "0+0+0+0";
        public int CashAmount { get; set; }
        public int EsewaAmount { get; set; }
        public int DueAmount { get; set; }
        public int Price { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? PhotoBase64 { get; set; }
        public string Category { get; set; } = "Children";
    }

    public class BoxingMemberUpdateRequest
    {
        public string Name { get; set; } = null!;
        public DateTime? JoinDate { get; set; }
        public string GuardianName { get; set; } = null!;
        public string GuardianContact { get; set; } = null!;
        public string PerMonthClass { get; set; } = "0+0+0+0";
        public int CashAmount { get; set; }
        public int EsewaAmount { get; set; }
        public int DueAmount { get; set; }
        public int Price { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? PhotoBase64 { get; set; }
        public string Category { get; set; } = "Children";
    }

    // ===================== LOCKER =====================
    public class LockerDto
    {
        public int LockerID { get; set; }
        public string LockerNumber { get; set; } = null!;
        public string Gender { get; set; } = "Gents";
        public string Status { get; set; } = "Empty";
        public int? CustomerID { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedPhone { get; set; }
        public string? Package { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int RentalMonths { get; set; }
        public decimal MonthlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string? Remarks { get; set; }
        public bool IsExpired { get; set; }
        public bool IsExpiringSoon { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class LockerAssignRequest
    {
        public int LockerID { get; set; }
        public string MemberName { get; set; } = null!;
        public string? Phone { get; set; }
        public int? CustomerID { get; set; }
        public string? Package { get; set; }
        public int Months { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
    }

    public class LockerRenewRequest
    {
        public int Months { get; set; }
        public decimal PaidAmount { get; set; }
    }

    public class LockerCreateRequest
    {
        public string LockerNumber { get; set; } = null!;
        public string Gender { get; set; } = "Gents";
        public string? Remarks { get; set; }
    }

    public class LockerInitRequest
    {
        public string Gender { get; set; } = "Gents";
        public int Count { get; set; }
    }

    // ===================== ATTENDANCE =====================
    public class AttendanceDto
    {
        public int AttendanceID { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string? Notes { get; set; }
    }

    public class QrCheckInRequest
    {
        public int CustomerID { get; set; }
        public string? QrToken { get; set; }
    }

    // ===================== SCHEDULE =====================
    public class ScheduleDto
    {
        public int ScheduleID { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string Gender { get; set; } = "Male";
        public string ClassName { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; } = "General";
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }

    public class ScheduleCreateRequest
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public string Gender { get; set; } = "Male";
        public string ClassName { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; } = "General";
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
