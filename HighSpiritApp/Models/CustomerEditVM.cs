namespace HighSpiritApp.Models;

public class CustomerEditVM
{
    // Customer
    public int CustomerID { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Height { get; set; }
    public decimal? WeightKG { get; set; }
    public string BloodGroup { get; set; }

    public string? Remarks { get; set; }
    public string? Shift { get; set; }


    // Membership (current)
    public int MembershipID { get; set; }
    public string PlanName { get; set; }
    public int PaidPrice { get; set; }
    public DateTime StartDate { get; set; }
    public int Duration { get; set; }
}
