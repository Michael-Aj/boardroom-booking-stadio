// Data/AppDbContext.cs (drop-in)
using BoardroomBooking4.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoardroomBooking4.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts)
       : IdentityDbContext<ApplicationUser>(opts)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSeries> BookingSeries => Set<BookingSeries>(); // NEW

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Venue
        b.Entity<Venue>(e =>
        {
            e.Property(v => v.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            e.HasMany(v => v.Bookings)
             .WithOne(bk => bk.Venue!)
             .HasForeignKey(bk => bk.VenueId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // BookingSeries (master)
        b.Entity<BookingSeries>(e =>
        {
            e.HasCheckConstraint("CK_Series_StartBeforeEnd", "StartUtc < EndUtc");

            e.Property(x => x.StartUtc)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v))
             .HasColumnType("INTEGER");
            e.Property(x => x.EndUtc)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v))
             .HasColumnType("INTEGER");
            e.Property(x => x.UntilUtc)
             .HasConversion(v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                            v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : (DateTimeOffset?)null)
             .HasColumnType("INTEGER");
        });

        // Booking (occurrence)
        b.Entity<Booking>(e =>
        {
            // Exact-duplicate guard (same venue + identical times)
            e.HasIndex(bk => new { bk.VenueId, bk.StartUtc, bk.EndUtc }).IsUnique();

            // Helpful for search
            e.HasIndex(bk => new { bk.VenueId, bk.StartUtc });

            e.HasCheckConstraint("CK_Booking_StartBeforeEnd", "StartUtc < EndUtc");

            e.Property(bk => bk.StartUtc)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v))
             .HasColumnType("INTEGER");
            e.Property(bk => bk.EndUtc)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v))
             .HasColumnType("INTEGER");

            // Link to series; cascade delete occurrences when series is removed
            e.HasOne(bk => bk.Series)
             .WithMany(s => s.Bookings)
             .HasForeignKey(bk => bk.SeriesId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Optional: keep your Venue.RowVersion bump here if you use it
    public override int SaveChanges() => base.SaveChanges();
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) => base.SaveChangesAsync(ct);
}
