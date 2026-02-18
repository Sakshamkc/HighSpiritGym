using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models
{
    public class GymSchedule
    {
        [Key]
        public int ScheduleID { get; set; }

        public string DayOfWeek { get; set; } = string.Empty; // "Day 1", "Day 2", "Day 3" (Male) or "Push Day", "Pull Day", "Leg Day" (Female)

        public string Gender { get; set; } = "Male"; // "Male" or "Female"

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
