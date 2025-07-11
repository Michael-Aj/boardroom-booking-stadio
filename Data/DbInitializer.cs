using BoardroomBooking4.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace BoardroomBooking4.Data;

public class DbInitializer(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles,
        IConfiguration cfg,
        ILogger<DbInitializer> log)
{
    public async Task SeedAsync()
    {
        // ---- Roles ---------------------------------
        const string adminRole = "Admin";
        if (!await roles.RoleExistsAsync(adminRole))
            await roles.CreateAsync(new IdentityRole(adminRole));

        // ---- Admin user ----------------------------
        //var email = cfg["Admin:User"]!;
        //var pass = cfg["Admin:Pass"]!;
        var email = cfg["Admin:User"] ?? throw new InvalidOperationException("Admin:User missing");
        var pass = cfg["Admin:Pass"] ?? throw new InvalidOperationException("Admin:Pass missing");

        var admin = await users.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (admin is null)
        {
            admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var res = await users.CreateAsync(admin, pass);
            //if (!res.Succeeded) throw new Exception(string.Join("; ", res.Errors));
            if (!res.Succeeded)
            {
                var details = string.Join("; ", res.Errors.Select(e => e.Description));
                throw new Exception(details);
            }
        }
        if (!await users.IsInRoleAsync(admin, adminRole))
            await users.AddToRoleAsync(admin, adminRole);

        // ---- sample data ---------------------------
        if (!await db.Venues.AnyAsync())
        {
            db.Venues.AddRange(
                new Venue { Name = "Boardroom Alpha", Capacity = 12, Location = "1st Floor – HQ, London" },
                new Venue { Name = "Boardroom Beta", Capacity = 8, Location = "2nd Floor – HQ, London" });
        }
        await db.SaveChangesAsync();
    }
}
