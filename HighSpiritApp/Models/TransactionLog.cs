using System.ComponentModel.DataAnnotations;

namespace HighSpiritApp.Models
{
    public class TransactionLog
    {
        [Key]
        public int Id { get; set; }
        public string TransactionType { get; set; } = ""; // Payment, Renewal, Refund
        public string EntityType { get; set; } = ""; // Customer, Boxing, Locker
        public int EntityId { get; set; }
        public string EntityName { get; set; } = "";
        public string? PlanName { get; set; }
        public decimal Amount { get; set; }
        public decimal DueAmount { get; set; }
        public string? PaymentMethod { get; set; } // Cash, eSewa, etc.
        public string? Description { get; set; }
        public string PerformedBy { get; set; } = "";
        public DateTime TransactionDate { get; set; }
    }
}
