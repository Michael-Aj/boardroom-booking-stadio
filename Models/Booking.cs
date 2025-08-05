using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardroomBooking4.Models;

public class Booking : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required]
    [Display(Name = "Start (UTC)")]
    public DateTimeOffset StartUtc { get; set; }

    [Required]
    [Display(Name = "End (UTC)")]
    public DateTimeOffset EndUtc { get; set; }

    [Required, StringLength(80)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [NotMapped]
    public DateTime LocalStart => StartUtc.LocalDateTime;

    // ---- server-side rule: End must be after Start ----
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndUtc <= StartUtc)
        {
            yield return new ValidationResult(
                "End time must be after start time.",
                new[] { nameof(EndUtc), nameof(StartUtc) }
            );
        }
    }
}
