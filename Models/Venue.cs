using System.ComponentModel.DataAnnotations;

namespace BoardroomBooking4.Models;

public class Venue
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Capacity { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    // optimistic concurrency
    public byte[]? RowVersion { get; set; }

    // nav
    public ICollection<Booking> Bookings { get; set; } = [];
}
