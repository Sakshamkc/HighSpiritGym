using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models
{
    public class GymSchedule
    {
        [Key]
        public int ScheduleID { get; set; }

        public string DayOfWeek { get; set; } = string.Empty; // "Monday", "Tuesday", etc.

        public string ClassName { get; set; } = string.Empty; // "Morning Cardio", "Weight Training", etc.

        public string StartTime { get; set; } = string.Empty; // "06:00"

        public string EndTime { get; set; } = string.Empty; // "07:00"

        public string? Instructor { get; set; }

        public string? Description { get; set; }

        public string Category { get; set; } = "General"; // "General", "Boxing", "Cardio", "Yoga", etc.

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;
    }
}
