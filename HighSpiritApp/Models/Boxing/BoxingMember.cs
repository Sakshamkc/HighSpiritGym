using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models.Boxing
{
    public class BoxingMember
    {
        [Key]
        public int BoxingMemberID { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public DateTime? JoinDate { get; set; }

        [Required]
        public string GuardianName { get; set; } = null!;

        [Required]
        public string GuardianContact { get; set; } = null!;

        public int Price { get; set; }

        public string? Remarks { get; set; }

        public byte[]? Photo { get; set; }
    }
}
