// Models/BookingSeries.cs
using System.ComponentModel.DataAnnotations;

namespace BoardroomBooking4.Models;

public enum RecurrenceFrequency
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}

public class BookingSeries
{
    public int Id { get; set; }

    [Required]
    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required, StringLength(80)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    // Seed occurrence window (first instance template)
    [Required] public DateTimeOffset StartUtc { get; set; }
    [Required] public DateTimeOffset EndUtc { get; set; }

    // Recurrence rule
    [Required]
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.None;

    // 1,2,3,... (e.g., every 2 weeks)
    [Range(1, 365)]
    public int Interval { get; set; } = 1;

    // Choose either Count or Until (both null => treat like single)
    [Range(1, 1000)]
    public int? Count { get; set; }        // number of occurrences (including the first)

    public DateTimeOffset? UntilUtc { get; set; } // last date/time boundary (inclusive)

    // Weekly: which days apply (stored as CSV: "Mon,Wed,Fri") — simple & SQLite friendly
    [StringLength(50)]
    public string? ByWeekdays { get; set; } // e.g., "Mon,Tue" (ignored for non-weekly)

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
