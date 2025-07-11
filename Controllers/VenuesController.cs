using BoardroomBooking4.Data;
using BoardroomBooking4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardroomBooking4.Controllers;

/// <summary>
/// Full CRUD for venues (Admin-only).
/// Concurrency is handled with RowVersion.
/// </summary>
[Authorize(Roles = "Admin")]
public class VenuesController(AppDbContext db) : Controller
{
    // ─────────────────────────────────────  LIST / DETAILS
    public async Task<IActionResult> Index() =>
        View(await db.Venues.AsNoTracking().ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var v = await db.Venues.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return v is null ? NotFound() : View(v);
    }

    // ─────────────────────────────────────  CREATE
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Venue v)
    {
        if (!ModelState.IsValid) return View(v);

        db.Venues.Add(v);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────  EDIT (concurrency)
    public async Task<IActionResult> Edit(int id)
    {
        var v = await db.Venues.FindAsync(id);
        return v is null ? NotFound() : View(v);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Venue form)
    {
        if (id != form.Id) return NotFound();
        if (!ModelState.IsValid) return View(form);

        db.Entry(form).Property(v => v.RowVersion).OriginalValue = form.RowVersion;

        try
        {
            db.Update(form);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "Another user updated this record. Reload and try again.");
            return View(form);
        }
    }

    // ─────────────────────────────────────  DELETE
    public async Task<IActionResult> Delete(int id)
    {
        var v = await db.Venues.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return v is null ? NotFound() : View(v);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var v = await db.Venues.FindAsync(id);
        if (v is not null)
        {
            db.Venues.Remove(v);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
