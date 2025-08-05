using BoardroomBooking4.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BoardroomBooking4.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts)
       : IdentityDbContext<ApplicationUser>(opts)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        /* ── Venue ───────────────────────────────────────────────────────── */
        // Concurrency token
        b.Entity<Venue>()
         .Property(v => v.RowVersion)
         .IsRowVersion();

        /* ── Booking ─────────────────────────────────────────────────────── */
        // Composite UNIQUE index: one booking per exact venue/start/end tuple
        b.Entity<Booking>()
         .HasIndex(bk => new { bk.VenueId, bk.StartUtc, bk.EndUtc })
         .IsUnique();

        // Logical constraint: End must be after Start
        b.Entity<Booking>()
         .ToTable(tb => tb.HasCheckConstraint(
             "CK_Bookings_StartBeforeEnd",
             "[EndUtc] > [StartUtc]"
         ));

        // SQLite cannot ORDER BY DateTimeOffset; store as INTEGER ticks instead
        if (Database.IsSqlite())
        {
            var dtoToLong = new DateTimeOffsetToBinaryConverter();

            b.Entity<Booking>()
             .Property(x => x.StartUtc)
             .HasConversion(dtoToLong);

            b.Entity<Booking>()
             .Property(x => x.EndUtc)
             .HasConversion(dtoToLong);
        }
    }
}
