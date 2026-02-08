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
        public string Gender { get; set; } = "Gents"; // Gents, Ladies

        public string Status { get; set; } = "Empty"; // Empty, Occupied, Locked, Maintenance

        // Assigned member info (nullable when not assigned)
        public int? CustomerID { get; set; }
        public string? AssignedTo { get; set; } // Member name
        public string? AssignedPhone { get; set; } // Member phone

        // Package/Plan linked - stores the full package name from customer
        public string? Package { get; set; } // e.g., "Custom 2 - Gym & Cardio", "Gym Package", etc.

        // Rental details
        public DateTime? StartDate { get; set; }  // Joined Date
        public DateTime? EndDate { get; set; }    // Expiry
        public int RentalMonths { get; set; }     // Duration in months
        public decimal MonthlyRate { get; set; }
        public decimal TotalAmount { get; set; }  // Amount
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }    // Due Amount

        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Computed properties
        public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.Today;
        public bool IsExpiringSoon => EndDate.HasValue && EndDate.Value >= DateTime.Today && EndDate.Value <= DateTime.Today.AddDays(7);
        public int DaysRemaining => EndDate.HasValue ? Math.Max(0, (EndDate.Value - DateTime.Today).Days) : 0;
    }
}
