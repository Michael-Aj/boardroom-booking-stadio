// Models/Booking.cs (only the new bits shown)
using System.ComponentModel.DataAnnotations;

namespace BoardroomBooking4.Models;

public class Booking : IValidatableObject
{
    public int Id { get; set; }

    public int? SeriesId { get; set; }          // NEW: link to series master (nullable)
    public BookingSeries? Series { get; set; }  // NEW: navigation

    [Required] public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required] public DateTimeOffset StartUtc { get; set; }
    [Required] public DateTimeOffset EndUtc { get; set; }

    [Required, StringLength(80)] public string Title { get; set; } = string.Empty;
    [StringLength(400)] public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StartUtc >= EndUtc)
            yield return new ValidationResult("Start time must be before end time.",
                new[] { nameof(StartUtc), nameof(EndUtc) });
    }
}
