using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardroomBooking4.Models
{
    public class Booking : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        [Required, DataType(DataType.DateTime)]
        public DateTimeOffset StartUtc { get; set; }

        [Required, DataType(DataType.DateTime)]
        public DateTimeOffset EndUtc { get; set; }

        [Required, StringLength(80)]
        public string Title { get; set; } = string.Empty;

        [StringLength(400)]
        public string? Description { get; set; }

        [NotMapped]
        public DateTime LocalStart => StartUtc.LocalDateTime;

        // === Model-level validation: ensures StartUtc < EndUtc ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartUtc >= EndUtc)
            {
                yield return new ValidationResult(
                    "Start time must be before end time.",
                    new[] { nameof(StartUtc), nameof(EndUtc) }
                );
            }
        }
    }
}
