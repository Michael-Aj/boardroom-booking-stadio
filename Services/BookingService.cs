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
///     Core domain logic for creating, updating, querying & deleting bookings,
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

    /// <summary>Compatibility wrapper so existing controller code still works.</summary>
    public Task<(bool ok, string? err)> CreateAsync(Booking b, CancellationToken ct = default)
        => BookAsync(b, ct);

    /// <summary>Create a booking (validates order and overlaps).</summary>
    public async Task<(bool ok, string? err)> BookAsync(Booking booking, CancellationToken ct = default)
    {
        // 1) Start must be before End (defense-in-depth; model also validates)
        if (booking.StartUtc >= booking.EndUtc)
            return (false, "Start time must be before end time.");

        // 2) Overlap check (exclude self if Id present)
        bool clashes = await _db.Bookings
            .Where(b => b.VenueId == booking.VenueId && b.Id != booking.Id)
            .AnyAsync(b => b.StartUtc < booking.EndUtc && booking.StartUtc < b.EndUtc, ct);

        if (clashes)
            return (false, "The selected time-slot is already taken.");

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>Update an existing booking (safe field update + overlap check).</summary>
    public async Task<(bool ok, string? err)> UpdateAsync(Booking model, CancellationToken ct = default)
    {
        // 1) Guard order
        if (model.StartUtc >= model.EndUtc)
            return (false, "Start time must be before end time.");

        // 2) Load the current entity
        var entity = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == model.Id, ct);
        if (entity is null)
            return (false, "Booking not found.");

        // 3) Overlap check (exclude self)
        bool clashes = await _db.Bookings
            .Where(b => b.VenueId == model.VenueId && b.Id != model.Id)
            .AnyAsync(b => b.StartUtc < model.EndUtc && model.StartUtc < b.EndUtc, ct);

        if (clashes)
            return (false, "The selected time-slot is already taken.");

        // 4) Apply allowed field changes (prevent over-posting)
        entity.Title = model.Title;
        entity.Description = model.Description;
        entity.VenueId = model.VenueId;
        entity.StartUtc = model.StartUtc;
        entity.EndUtc = model.EndUtc;

        try
        {
            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "This booking was modified by another user. Please reload and try again.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error updating booking {Id}", model.Id);
            return (false, "Unable to update booking.");
        }
    }

    /// <summary>Needed by <c>BookingsController.DeleteConfirmed</c>.</summary>
    public async Task DeleteAsync(Booking booking, CancellationToken ct = default)
    {
        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync(ct);
    }

    /* ─────────────────────────  LIST / FILTER  ──────────────────────────── */

    /// <summary>Alias so controller can call <c>Filter()</c> as before.</summary>
    public IQueryable<Booking> Filter(int? venueId, DateOnly? date) => Search(venueId, date);

    /// <summary>List/search; filters by venue and specific date (UTC day window).</summary>
    public IQueryable<Booking> Search(int? venueId, DateOnly? date)
    {
        IQueryable<Booking> q = _db.Bookings
            .AsNoTracking()          // read-only
            .Include(b => b.Venue);  // eager-load venue info

        if (venueId is not null)
            q = q.Where(b => b.VenueId == venueId);

        if (date is not null)
        {
            // Convert the supplied DateOnly into the full UTC day range [00:00..23:59:59.999]
            DateOnly d = date.Value;

            // If targeting .NET 8, the DateOnly/TimeOnly overload exists:
            var fromUtc = new DateTimeOffset(d, TimeOnly.MinValue, TimeSpan.Zero);
            var toUtc = new DateTimeOffset(d, TimeOnly.MaxValue, TimeSpan.Zero);

            // If you want "any booking touching this day" use:
            // q = q.Where(b => b.StartUtc < toUtc && fromUtc < b.EndUtc);
            // Current behavior: bookings whose Start falls within the day.
            q = q.Where(b => b.StartUtc >= fromUtc && b.StartUtc <= toUtc);
        }

        return q;
    }
}
