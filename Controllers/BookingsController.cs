using BoardroomBooking4.Models;
using BoardroomBooking4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardroomBooking4.Controllers;

/// <summary>Public list + Admin CRUD for bookings.</summary>
public class BookingsController(BookingService svc) : Controller
{
    // Helper: trim seconds & milliseconds, preserve offset
    private static DateTimeOffset TrimToMinute(DateTimeOffset dt) =>
        new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Offset);

    /*────────────────────────────  LIST / SEARCH ────────────────────────────*/
    public async Task<IActionResult> Index(int? venueId, DateOnly? date,
                                           CancellationToken ct)
    {
        ViewData["Venues"] = await svc.GetVenuesAsync(ct);
        ViewData["SelectedVenue"] = venueId;
        ViewData["SelectedDate"] = date;

        var items = await svc.Filter(venueId, date).ToListAsync(ct);
        return View(items);
    }

    /*────────────────────────────  DETAILS ────────────────────────────────*/
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await svc.Filter(null, null)
                            .Include(b => b.Venue)
                            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return item is null ? NotFound() : View(item);
    }

    /*────────────────────────────  CREATE ────────────────────────────────*/
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Venues"] = await svc.GetVenuesAsync(ct);

        var now = TrimToMinute(DateTimeOffset.UtcNow);
        return View(new Booking
        {
            StartUtc = now,
            EndUtc = now.AddHours(1)
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Booking model, CancellationToken ct)
    {
        // Normalize to minute (drop seconds/ms)
        model.StartUtc = TrimToMinute(model.StartUtc);
        model.EndUtc = TrimToMinute(model.EndUtc);

        // Defensive check (Booking implements IValidatableObject too)
        if (model.EndUtc <= model.StartUtc)
        {
            ModelState.AddModelError(string.Empty, "End time must be after start time.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Venues"] = await svc.GetVenuesAsync(ct);
            return View(model);
        }

        var (ok, err) = await svc.CreateAsync(model, ct);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, err ?? "Unable to book.");
            ViewData["Venues"] = await svc.GetVenuesAsync(ct);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    /*────────────────────────────  EDIT ────────────────────────────────*/
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var item = await svc.Filter(null, null)
                            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (item is null) return NotFound();

        ViewData["Venues"] = await svc.GetVenuesAsync(ct);
        return View(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Booking model, CancellationToken ct)
    {
        if (id != model.Id) return NotFound();

        // Normalize to minute (drop seconds/ms)
        model.StartUtc = TrimToMinute(model.StartUtc);
        model.EndUtc = TrimToMinute(model.EndUtc);

        // Defensive check (in addition to model validation)
        if (model.EndUtc <= model.StartUtc)
        {
            ModelState.AddModelError(string.Empty, "End time must be after start time.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Venues"] = await svc.GetVenuesAsync(ct);
            return View(model);
        }

        var (ok, err) = await svc.UpdateAsync(model, ct);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, err ?? "Unable to update booking.");
            ViewData["Venues"] = await svc.GetVenuesAsync(ct);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    /*────────────────────────────  DELETE ───────────────────────────────*/
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await svc.Filter(null, null)
                            .Include(b => b.Venue)
                            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return item is null ? NotFound() : View(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var item = await svc.Filter(null, null)
                            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (item is not null)
            await svc.DeleteAsync(item, ct);

        return RedirectToAction(nameof(Index));
    }
}
