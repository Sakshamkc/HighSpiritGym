using System.ComponentModel.DataAnnotations.Schema;

namespace HighSpiritApp.Models
{

    public class CustomerMembership
    {
        public int MembershipID { get; set; }
        public int CustomerID { get; set; }

        public string? PlanName { get; set; }
        public int PaidPrice { get; set; }
        public int DueAmount { get; set; } // Amount yet to be paid
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }   // months
        public bool IsActive { get; set; }

        public DateTime ExpireDate { get; set; }

        public int DueDaysComputed { get; private set; }

        // Computed property for total price
        [NotMapped]
        public int TotalPrice => PaidPrice + DueAmount;

        public Customer Customer { get; set; }
    }

}
