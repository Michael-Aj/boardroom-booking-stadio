using System.ComponentModel.DataAnnotations;

namespace BoardroomBooking4.ViewModels;

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool Remember { get; set; }

    /// <summary>Where to redirect after successful sign-in.</summary>
    public string? ReturnUrl { get; set; }
}
