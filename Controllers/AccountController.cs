using BoardroomBooking4.Models;
using BoardroomBooking4.ViewModels;        // ← reference the canonical VM
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardroomBooking4.Controllers;

/// <summary>Minimal email-password sign-in for the single Admin account.</summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ILogger<AccountController> _log;

    public AccountController(SignInManager<ApplicationUser> signIn,
                             ILogger<AccountController> log)
    {
        _signIn = signIn;
        _log = log;
    }

    /* ─────────────────────────────── LOGIN ─────────────────────────────── */

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginVm { ReturnUrl = returnUrl ?? "/" });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signIn.PasswordSignInAsync(
                         vm.Email!,            // user name (email)
                         vm.Password!,         // password
                         vm.Remember,          // persistent cookie?
                         lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials");
            return View(vm);
        }
        return LocalRedirect(vm.ReturnUrl ?? "/");
    }

    /* ─────────────────────────────── LOGOUT ────────────────────────────── */

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    /* ─────────────────────────── ACCESS-DENIED ─────────────────────────── */

    public IActionResult AccessDenied() => View();
}
