// Services/BookingService.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using BoardroomBooking4.Data;
using BoardroomBooking4.Models;
using BoardroomBooking4.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardroomBooking4.Services;

/// <summary>
///     Core domain logic for creating, updating, querying & deleting bookings,
///     plus helper utilities for the UI. Adds support for recurring series.
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

    /* ─────────────────────  ONE-TIME CREATE / UPDATE / DELETE  ───────────── */

    /// <summary>Compatibility wrapper so existing controller code still works.</summary>
    public Task<(bool ok, string? err)> CreateAsync(Booking b, CancellationToken ct = default)
        => BookAsync(b, ct);

    /// <summary>Create a single booking (validates time order and overlaps).</summary>
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

    /* ─────────────────────  RECURRING SERIES (NEW)  ─────────────────────── */

    /// <summary>
    /// Create a one-time or recurring set of bookings in a single transaction.
    /// If Frequency == None, creates a single booking (delegates to CreateAsync).
    /// </summary>
    public async Task<(bool ok, string? err)> CreateSeriesAsync(
        BookingCreateViewModel vm, CancellationToken ct = default)
    {
        // Single booking path
        if (vm.Frequency == RecurrenceFrequency.None)
        {
            return await CreateAsync(new Booking
            {
                VenueId = vm.VenueId,
                Title = vm.Title,
                Description = vm.Description,
                StartUtc = vm.StartUtc,
                EndUtc = vm.EndUtc
            }, ct);
        }

        // Guards
        if (vm.StartUtc >= vm.EndUtc)
            return (false, "Start time must be before end time.");
        if ((vm.Count is null || vm.Count <= 0) && vm.UntilUtc is null)
            return (false, "Provide either a Count or an Until date for recurrence.");
        if (vm.Interval < 1) vm.Interval = 1;

        // Build master series (ByWeekdays as "Mon,Wed" CSV if Weekly)
        var series = new BookingSeries
        {
            VenueId = vm.VenueId,
            Title = vm.Title,
            Description = vm.Description,
            StartUtc = vm.StartUtc,
            EndUtc = vm.EndUtc,
            Frequency = vm.Frequency,
            Interval = vm.Interval,
            Count = vm.Count,
            UntilUtc = vm.UntilUtc,
            ByWeekdays = vm.Frequency == RecurrenceFrequency.Weekly && vm.WeeklyDays.Any()
                         ? string.Join(',', vm.WeeklyDays.Select(d => d.ToString().Substring(0, 3))) // "Mon,Wed"
                         : null
        };

        // Expand to candidate occurrences
        var occurrences = GenerateOccurrences(series).ToList();
        if (occurrences.Count == 0)
            return (false, "No occurrences generated. Check Count/Until settings.");

        // Efficient overlap window check
        var minStart = occurrences.Min(o => o.StartUtc);
        var maxEnd = occurrences.Max(o => o.EndUtc);

        var existing = await _db.Bookings
            .Where(b => b.VenueId == series.VenueId &&
                        b.StartUtc < maxEnd &&
                        minStart < b.EndUtc)
            .Select(b => new { b.StartUtc, b.EndUtc })
            .ToListAsync(ct);

        foreach (var o in occurrences)
        {
            if (existing.Any(e => e.StartUtc < o.EndUtc && o.StartUtc < e.EndUtc))
                return (false, "One or more occurrences overlap existing bookings. No changes saved.");
        }

        // Save atomically
        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.BookingSeries.Add(series);
            await _db.SaveChangesAsync(ct); // get Series.Id

            foreach (var o in occurrences)
            {
                o.SeriesId = series.Id;
                _db.Bookings.Add(o);
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _log.LogError(ex, "Error creating recurring series");
            return (false, "Failed to create recurring bookings.");
        }
    }

    /// <summary>
    /// Expands a BookingSeries into concrete Booking instances (not tracked).
    /// Daily/Weekly/Monthly supported. Uses UTC Start/End (as per your model).
    /// </summary>
    private IEnumerable<Booking> GenerateOccurrences(BookingSeries s)
    {
        var span = s.EndUtc - s.StartUtc;
        if (span <= TimeSpan.Zero) yield break;

        int emitted = 0;

        // Helpers
        bool hitCount() => s.Count.HasValue && emitted >= s.Count.Value;
        bool withinUntil(DateTimeOffset start) => !s.UntilUtc.HasValue || start <= s.UntilUtc.Value;

        // Weekly day set (Mon, Tue, ...)
        HashSet<DayOfWeek>? weekly = null;
        if (s.Frequency == RecurrenceFrequency.Weekly)
        {
            weekly = ParseWeekdaysCsv(s.ByWeekdays);
            if (weekly.Count == 0) weekly.Add(s.StartUtc.DayOfWeek); // default to seed's weekday
        }

        // Cursor always represents the next candidate "period"
        var cursor = s.StartUtc;

        switch (s.Frequency)
        {
            case RecurrenceFrequency.Daily:
                while (true)
                {
                    if (hitCount()) yield break;
                    if (!withinUntil(cursor)) yield break;

                    yield return NewOccurrence(s, cursor, cursor + span);
                    emitted++;
                    cursor = cursor.AddDays(s.Interval);
                }

            case RecurrenceFrequency.Weekly:
                {
                    // For weekly, iterate weeks; within each week, emit selected days
                    var seedDOW = s.StartUtc.DayOfWeek;
                    var weekCursor = s.StartUtc; // start week aligned to first occurrence week

                    while (true)
                    {
                        // Emit selected days in this week, at the seed's wall-clock time
                        var weekStartDate = weekCursor.Date; // date-only
                        // iterate 7 days from weekStartDate
                        for (int i = 0; i < 7; i++)
                        {
                            var day = weekStartDate.AddDays(i);
                            var dow = day.DayOfWeek;
                            if (!weekly.Contains(dow)) continue;

                            var start = new DateTimeOffset(day.Year, day.Month, day.Day,
                                                           s.StartUtc.Hour, s.StartUtc.Minute, s.StartUtc.Second,
                                                           s.StartUtc.Offset);
                            if (start < s.StartUtc) continue; // don’t emit before seed
                            if (hitCount()) yield break;
                            if (!withinUntil(start)) yield break;

                            yield return NewOccurrence(s, start, start + span);
                            emitted++;
                        }

                        // advance N weeks
                        weekCursor = weekCursor.AddDays(7 * s.Interval);
                    }
                }

            case RecurrenceFrequency.Monthly:
                while (true)
                {
                    if (hitCount()) yield break;
                    if (!withinUntil(cursor)) yield break;

                    yield return NewOccurrence(s, cursor, cursor + span);
                    emitted++;
                    cursor = AddMonthsSafe(cursor, s.Interval);
                }

            default: // None (should be handled earlier; keep for safety)
                yield return NewOccurrence(s, s.StartUtc, s.EndUtc);
                yield break;
        }
    }

    private static Booking NewOccurrence(BookingSeries s, DateTimeOffset start, DateTimeOffset end) =>
        new()
        {
            VenueId = s.VenueId,
            Title = s.Title,
            Description = s.Description,
            StartUtc = start,
            EndUtc = end
        };

    private static HashSet<DayOfWeek> ParseWeekdaysCsv(string? csv)
    {
        var set = new HashSet<DayOfWeek>();
        if (string.IsNullOrWhiteSpace(csv)) return set;

        foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var t = token.ToLowerInvariant();
            if (t.StartsWith("mon")) set.Add(DayOfWeek.Monday);
            else if (t.StartsWith("tue")) set.Add(DayOfWeek.Tuesday);
            else if (t.StartsWith("wed")) set.Add(DayOfWeek.Wednesday);
            else if (t.StartsWith("thu")) set.Add(DayOfWeek.Thursday);
            else if (t.StartsWith("fri")) set.Add(DayOfWeek.Friday);
            else if (t.StartsWith("sat")) set.Add(DayOfWeek.Saturday);
            else if (t.StartsWith("sun")) set.Add(DayOfWeek.Sunday);
        }
        return set;
    }

    private static DateTimeOffset AddMonthsSafe(DateTimeOffset start, int months)
    {
        // DateTimeOffset.AddMonths already clamps end-of-month correctly (e.g., Jan 31 -> Feb 28)
        return start.AddMonths(months);
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

            // .NET 8 DateOnly/TimeOnly overload:
            var fromUtc = new DateTimeOffset(d, TimeOnly.MinValue, TimeSpan.Zero);
            var toUtc = new DateTimeOffset(d, TimeOnly.MaxValue, TimeSpan.Zero);

            // Current behavior: bookings whose Start falls within the day.
            // For "any booking touching this day", use: b.StartUtc < toUtc && fromUtc < b.EndUtc
            q = q.Where(b => b.StartUtc >= fromUtc && b.StartUtc <= toUtc);
        }

        return q;
    }
}
