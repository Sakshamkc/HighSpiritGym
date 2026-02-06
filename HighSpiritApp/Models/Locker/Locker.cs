using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models.Locker
{
    /// <summary>
    /// Locker entity for gym locker management
    /// </summary>
    public class Locker
    {
        [Key]
        public int LockerID { get; set; }

        [Required]
        [StringLength(20)]
        public string LockerNumber { get; set; } = null!;

        [Required]
        public string Size { get; set; } = "Medium"; // Small, Medium, Large

        public string Status { get; set; } = "Available"; // Available, Occupied, Maintenance

        // Assigned member info (nullable when not assigned)
        public int? CustomerID { get; set; }
        public string? AssignedTo { get; set; } // Member name
        public string? AssignedPhone { get; set; } // Member phone

        // Rental details
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int RentalMonths { get; set; }
        public decimal MonthlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }

        // Key management
        public string? KeyNumber { get; set; }
        public bool KeyDeposit { get; set; } // Whether key deposit was taken
        public decimal KeyDepositAmount { get; set; }

        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Computed properties
        public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.Today;
        public bool IsExpiringSoon => EndDate.HasValue && EndDate.Value >= DateTime.Today && EndDate.Value <= DateTime.Today.AddDays(7);
        public int DaysRemaining => EndDate.HasValue ? Math.Max(0, (EndDate.Value - DateTime.Today).Days) : 0;
    }
}
