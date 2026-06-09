using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models
{
    public class DeviceToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        public int? CustomerID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}
