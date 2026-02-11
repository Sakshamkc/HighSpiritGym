using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models.Attendance
{
    /// <summary>
    /// Attendance record for gym check-in/check-out via QR code
    /// </summary>
    public class AttendanceRecord
    {
        [Key]
        public int AttendanceID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = null!;

        public DateTime PunchInTime { get; set; }
        public DateTime? PunchOutTime { get; set; }

        public DateOnly Date { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public Customer? Customer { get; set; }

        // Computed properties
        public bool IsPunchedOut => PunchOutTime.HasValue;
        public string Duration
        {
            get
            {
                if (!PunchOutTime.HasValue) return "Still In";
                var span = PunchOutTime.Value - PunchInTime;
                return $"{(int)span.TotalHours}h {span.Minutes}m";
            }
        }
    }
}
