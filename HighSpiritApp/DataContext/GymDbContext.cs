using HighSpiritApp.Models;
using HighSpiritApp.Models.Boxing;
using HighSpiritApp.Models.Locker;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.DataContext
{
    public class GymDbContext : DbContext
    {
        public GymDbContext(DbContextOptions<GymDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerMembership> CustomerMemberships { get; set; }
        public DbSet<BoxingMember> BoxingMembers { get; set; }
        public DbSet<Locker> Lockers { get; set; }

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

            // Locker configuration
            modelBuilder.Entity<Locker>(entity =>
            {
                entity.HasKey(l => l.LockerID);
                entity.Property(l => l.LockerNumber).IsRequired().HasMaxLength(20);
                entity.Property(l => l.Gender).IsRequired().HasMaxLength(10).HasDefaultValue("Gents");
                entity.Property(l => l.Package).HasMaxLength(200);
                
                // Unique constraint on LockerNumber + Gender
                entity.HasIndex(l => new { l.LockerNumber, l.Gender }).IsUnique();
                
                entity.Property(l => l.MonthlyRate).HasPrecision(10, 2);
                entity.Property(l => l.TotalAmount).HasPrecision(10, 2);
                entity.Property(l => l.PaidAmount).HasPrecision(10, 2);
                entity.Property(l => l.DueAmount).HasPrecision(10, 2);
            });
        }
    }
}
