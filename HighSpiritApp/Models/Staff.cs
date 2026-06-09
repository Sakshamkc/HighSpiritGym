using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models
{
    public class Staff
    {
        [Key]
        public int StaffID { get; set; }

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Gender { get; set; }

        public int? Age { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; } // e.g. Trainer, Receptionist, Cleaner

        public byte[]? Photo { get; set; }

        [MaxLength(64)]
        public string? QrToken { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime JoinDate { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
