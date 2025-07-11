using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoardroomBooking4.Models;

public class Booking
{
    public int Id { get; set; }

    [Required]
    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required] public DateTimeOffset StartUtc { get; set; }
    [Required] public DateTimeOffset EndUtc { get; set; }

    [Required, StringLength(80)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [NotMapped]
    public DateTime LocalStart => StartUtc.LocalDateTime;
}
