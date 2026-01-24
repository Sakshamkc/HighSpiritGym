using System.ComponentModel.DataAnnotations.Schema;

namespace HighSpiritApp.Models
{

    public class CustomerMembership
    {
        public int MembershipID { get; set; }
        public int CustomerID { get; set; }

        public string? PlanName { get; set; }
        public int PaidPrice { get; set; }
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }   // months
        public bool IsActive { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? ExpireDate { get; private set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int DueDaysComputed { get; private set; }

        public Customer Customer { get; set; }
    }

}
