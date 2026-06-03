using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, Renewed

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // Customer, Membership, Boxing, Locker

        public int EntityId { get; set; }

        [MaxLength(200)]
        public string EntityName { get; set; } = string.Empty; // e.g. "Aaditya Dhakal"

        [MaxLength(500)]
        public string? Description { get; set; } // e.g. "Created customer Aaditya Dhakal with Gym plan"

        [Required]
        [MaxLength(100)]
        public string PerformedBy { get; set; } = string.Empty; // username

        public DateTime PerformedAt { get; set; } = DateTime.Now;
    }
}
