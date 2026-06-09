using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models
{
    public class StaffAttendance
    {
        [Key]
        public int Id { get; set; }

        public int StaffID { get; set; }

        [MaxLength(200)]
        public string StaffName { get; set; } = string.Empty;

        public DateTime CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation
        public Staff? Staff { get; set; }
    }
}
