// Services/BookingService.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using BoardroomBooking4.Data;
using BoardroomBooking4.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardroomBooking4.Services;

/// <summary>
///     Core domain logic for creating & querying bookings,
///     plus helper utilities for the UI.
/// </summary>
public class BookingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BookingService> _log;

    public BookingService(AppDbContext db, ILogger<BookingService> log)
        => (_db, _log) = (db, log);

    /* ───────────────────────────────  VENUES  ────────────────────────────── */

    public async Task<IReadOnlyList<Venue>> GetVenuesAsync(CancellationToken ct = default) =>
        await _db.Venues.AsNoTracking().OrderBy(v => v.Name).ToListAsync(ct);

    public async Task<SelectList> GetVenueSelectListAsync(
        int? selectedId = null, CancellationToken ct = default)
        => new(await GetVenuesAsync(ct), "Id", "Name", selectedId);

    /* ───────────────────────  CREATE / UPDATE / DELETE  ─────────────────── */

    /// <summary>**New** wrapper so existing controller code still works.</summary>
    public Task<(bool ok, string? err)> CreateAsync(Booking b, CancellationToken ct = default)
        => BookAsync(b, ct);

    /// <summary>Main create routine (formerly <c>BookAsync</c>).</summary>
    public async Task<(bool ok, string? err)> BookAsync(Booking booking, CancellationToken ct = default)
    {
        bool clashes = await _db.Bookings
            .Where(b => b.VenueId == booking.VenueId && b.Id != booking.Id)
            .AnyAsync(b => b.StartUtc < booking.EndUtc && booking.StartUtc < b.EndUtc, ct);

        if (clashes)
            return (false, "The selected time-slot is already taken.");

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>Needed by <c>BookingsController.DeleteConfirmed</c>.</summary>
    public async Task DeleteAsync(Booking booking, CancellationToken ct = default)
    {
        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync(ct);
    }

    /* ─────────────────────────  LIST / FILTER  ──────────────────────────── */

    /// <summary>**Alias** so controller can call <c>Filter()</c> as before.</summary>
    public IQueryable<Booking> Filter(int? venueId, DateOnly? date) => Search(venueId, date);

    /// <summary>Actual implementation.</summary>
    public IQueryable<Booking> Search(int? venueId, DateOnly? date)
    {
        IQueryable<Booking> q = _db.Bookings
            .AsNoTracking()          // read-only
            .Include(b => b.Venue);  // eager-load venue info

        if (venueId is not null)
            q = q.Where(b => b.VenueId == venueId);

        if (date is not null)
        {
            // Convert the supplied DateOnly into a full-day UTC range
            DateOnly d = date.Value;
            var fromUtc = new DateTimeOffset(d, TimeOnly.MinValue, TimeSpan.Zero); // 00:00 UTC
            var toUtc = new DateTimeOffset(d, TimeOnly.MaxValue, TimeSpan.Zero); // 23:59:59.999 UTC

            q = q.Where(b => b.StartUtc >= fromUtc && b.StartUtc <= toUtc);
        }

        return q;
    }
}


