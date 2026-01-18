namespace HighSpiritApp.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public string? BloodGroup { get; set; }
        public decimal? WeightKG { get; set; }
        public string? Height { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime DateOfBirth { get; set; }
        public byte[]? Photo { get; set; }

        public ICollection<CustomerMembership> Memberships { get; set; }
    }
}
