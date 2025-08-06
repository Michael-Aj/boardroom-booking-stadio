// ViewModels/BookingCreateViewModel.cs
using System.ComponentModel.DataAnnotations;
using BoardroomBooking4.Models;

namespace BoardroomBooking4.ViewModels;

public class BookingCreateViewModel
{
    // Base booking fields
    [Required] public int VenueId { get; set; }
    [Required, StringLength(80)] public string Title { get; set; } = string.Empty;
    [StringLength(400)] public string? Description { get; set; }
    [Required] public DateTimeOffset StartUtc { get; set; }
    [Required] public DateTimeOffset EndUtc { get; set; }

    // Recurrence
    [Required] public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.None;
    [Range(1, 365)] public int Interval { get; set; } = 1;
    [Range(1, 1000)] public int? Count { get; set; }
    public DateTimeOffset? UntilUtc { get; set; }

    // Weekly only: selected days
    public List<DayOfWeek> WeeklyDays { get; set; } = new();
}
