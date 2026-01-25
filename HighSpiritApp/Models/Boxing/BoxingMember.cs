using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models.Boxing
{
    public class BoxingMember
    {
        [Key]
        public int BoxingMemberID { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public DateTime? JoinDate { get; set; }

        [Required]
        public string GuardianName { get; set; } = null!;

        [Required]
        public string GuardianContact { get; set; } = null!;

        // 📊 Excel-style fields
        [Required]
        public string PerMonthClass { get; set; } = "0+0+0+0"; // e.g. 1+1+1+1

        public int CashAmount { get; set; }

        public int EsewaAmount { get; set; }

        public int DueAmount { get; set; }

        public int Price { get; set; }   // total monthly fee (optional but useful)

        public string? Remarks { get; set; }

        public byte[]? Photo { get; set; }
    }

}
