using BoardroomBooking4.Models;
using BoardroomBooking4.Services;
using BoardroomBooking4.ViewModels;                 // <-- NEW
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardroomBooking4.Controllers;

/// <summary>Public list + Admin CRUD for bookings.</summary>
public class BookingsController(BookingService svc) : Controller
{
    /*────────────────────────────  LIST / SEARCH ────────────────────────────*/
    public async Task<IActionResult> Index(int? venueId, DateOnly? date, CancellationToken ct)
    {
        ViewData["Venues"] = await svc.GetVenuesAsync(ct);
        ViewData["SelectedVenue"] = venueId;
        ViewData["SelectedDate"] = date;

        var items = await svc.Filter(venueId, date)
                             .AsNoTracking()
                             .OrderBy(b => b.StartUtc)   // sort in DB
                             .ToListAsync(ct);
        return View(items);
    }

    /*────────────────────────────  DETAILS ────────────────────────────────*/
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await svc.Filter(null, null)
                            .AsNoTracking()
                            .Include(b => b.Venue)
                            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return item is null ? NotFound() : View(item);
    }

    /*────────────────────────────  CREATE (wired to VM/series) ──────────────*/
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Venues"] = await svc.GetVenuesAsync(ct);

        // Use the recurrence view-model with sensible defaults
        return View(new BookingCreateViewModel
        {
            StartUtc = DateTimeOffset.UtcNow,
            EndUtc = DateTimeOffset.UtcNow.AddHours(1),
            Frequency = RecurrenceFrequency.None,
            Interval = 1
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model, CancellationToken ct)
    {
        // Guard: Start must be before End (extra safety; service also checks)
        if (model.StartUtc >= model.EndUtc)
            ModelState.AddModelError(string.Empty, "Start time must be before end time.");

        if (!ModelState.IsValid)
        {
            ViewData["Venues"] = await svc.GetVenuesAsync(ct);
            return View(model);
        }

        // Create one-time or recurring series (atomic)
        var (ok, err) = await svc.CreateSeriesAsync(model, ct);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, err ?? "Unable to create booking(s).");
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
                            .AsNoTracking()
                            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (item is null) return NotFound();

        ViewData["Venues"] = await svc.GetVenuesAsync(ct);
        return View(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Booking model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();

        // Guard: Start must be before End
        if (model.StartUtc >= model.EndUtc)
            ModelState.AddModelError(string.Empty, "Start time must be before end time.");

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
                            .AsNoTracking()
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
