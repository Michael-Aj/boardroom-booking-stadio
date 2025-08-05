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

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ── Venue config ───────────────────────────────────────────────────
        b.Entity<Venue>(e =>
        {
            // SQLite has no native rowversion; mark as concurrency token and
            // we'll bump it manually in SaveChanges (see overrides below).
            e.Property(v => v.RowVersion)
             .IsConcurrencyToken()
             .ValueGeneratedNever();

            e.HasMany(v => v.Bookings)
             .WithOne(bk => bk.Venue!)
             .HasForeignKey(bk => bk.VenueId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Booking config ─────────────────────────────────────────────────
        b.Entity<Booking>(e =>
        {
            // Prevent identical duplicates (same venue + identical start/end)
            e.HasIndex(bk => new { bk.VenueId, bk.StartUtc, bk.EndUtc })
             .IsUnique();

            // Helpful for searching by venue/day
            e.HasIndex(bk => new { bk.VenueId, bk.StartUtc });

            // DB-level guard: Start before End (SQLite syntax: no brackets)
            e.HasCheckConstraint("CK_Booking_StartBeforeEnd", "StartUtc < EndUtc");

            // Store DateTimeOffset as epoch milliseconds (INTEGER in SQLite)
            e.Property(bk => bk.StartUtc)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v))
             .HasColumnType("INTEGER");

            e.Property(bk => bk.EndUtc)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v))
             .HasColumnType("INTEGER");
        });
    }

    // ── Concurrency token bump for SQLite (RowVersion) ─────────────────────
    public override int SaveChanges()
    {
        BumpVenueRowVersions();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        BumpVenueRowVersions();
        return base.SaveChangesAsync(ct);
    }

    private void BumpVenueRowVersions()
    {
        // Ensure the token is never null and changes on every update
        foreach (var entry in ChangeTracker.Entries<Venue>()
                     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            entry.Property(nameof(Venue.RowVersion)).CurrentValue = Guid.NewGuid().ToByteArray();
        }
    }
}
