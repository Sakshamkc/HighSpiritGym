using HighSpiritApp.Models;
using HighSpiritApp.Models.Boxing;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.DataContext
{
    public class GymDbContext : DbContext
    {
        public GymDbContext(DbContextOptions<GymDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerMembership> CustomerMemberships { get; set; }
        public DbSet<BoxingMember> BoxingMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                .HasKey(c => c.CustomerID);

            modelBuilder.Entity<CustomerMembership>()
                .HasKey(m => m.MembershipID);

            modelBuilder.Entity<CustomerMembership>()
                .HasOne(m => m.Customer)
                .WithMany(c => c.Memberships)
                .HasForeignKey(m => m.CustomerID);

            modelBuilder.Entity<CustomerMembership>(entity =>
            {
                entity.Property(e => e.ExpireDate)
                      .ValueGeneratedNever();

                entity.Property(e => e.DueDaysComputed)
                      .ValueGeneratedNever();
            });
        }
    }
}
