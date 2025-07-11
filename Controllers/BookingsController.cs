using BoardroomBooking4.Models;
using BoardroomBooking4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardroomBooking4.Controllers;

/// <summary>Public list + Admin CRUD for bookings.</summary>
public class BookingsController(BookingService svc) : Controller
{
    /*────────────────────────────  LIST / SEARCH ────────────────────────────*/
    public async Task<IActionResult> Index(int? venueId, DateOnly? date,
                                           CancellationToken ct)
    {
        ViewData["Venues"] = await svc.GetVenuesAsync(ct);
        ViewData["SelectedVenue"] = venueId;
        ViewData["SelectedDate"] = date;

        var items = (await svc.Filter(venueId, date)
                      .ToListAsync(ct))      // materialise first
            .OrderBy(b => b.StartUtc)       // then sort in memory
            .ToList();
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
        return View(new Booking
        {
            StartUtc = DateTimeOffset.UtcNow,
            EndUtc = DateTimeOffset.UtcNow.AddHours(1)
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Booking model, CancellationToken ct)
    {
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
