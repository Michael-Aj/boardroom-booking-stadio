using BoardroomBooking4.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;

namespace BoardroomBooking4.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts)
       : IdentityDbContext<ApplicationUser>(opts)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // concurrency token
        b.Entity<Venue>()
         .Property(v => v.RowVersion)
         .IsRowVersion();

        // composite unique: one booking per venue/time
        b.Entity<Booking>()
         .HasIndex(bk => new { bk.VenueId, bk.StartUtc, bk.EndUtc });
    }
}
