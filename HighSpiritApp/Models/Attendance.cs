using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceID { get; set; }

        public int CustomerID { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public DateTime CheckInTime { get; set; } = DateTime.Now;

        public DateTime? CheckOutTime { get; set; }

        public string? Notes { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
    }
}
