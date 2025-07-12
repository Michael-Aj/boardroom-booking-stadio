using BoardroomBooking4.Data;
using BoardroomBooking4.Models;
using BoardroomBooking4.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// ——————————————————————————————
// 1.  SERVICE REGISTRATION (before Build)
// ——————————————————————————————

//// 1a. DbContext from appsettings.json
//builder.Services.AddDbContext<AppDbContext>(opts =>
//    opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1b. DbContext override that points at an explicit file path
var dbPath = Environment.GetEnvironmentVariable("DB_PATH")
             ?? Path.Combine(builder.Environment.ContentRootPath, "data", "boardroom-dev.db");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}"));

// (commented-out registrations left exactly as provided)
// 2. Identity (add default token providers + roles if you wish)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// MVC + Razor
builder.Services.AddControllersWithViews();

// domain services
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<DbInitializer>();
// 1️⃣  Persist DP keys
var keysPath = Environment.GetEnvironmentVariable("DP_KEYS_PATH")
              ?? Path.Combine(builder.Environment.ContentRootPath, "data", "keys");

builder.Services.AddDataProtection()
       .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
       .SetApplicationName("BoardroomBooking4");   // share keys only across this app

// ——————————————————————————————
// 2.  BUILD THE APP (collection is now read-only)
// ——————————————————————————————
var app = builder.Build();


// ——————————————————————————————
// 3.  POST-BUILD WORK (migrations, seeding, admin user)
// ——————————————————————————————
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        var seeder = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        log.LogError(ex, "An error occurred while migrating / seeding the database.");
        throw;
    }
}

var adminPw = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
             ?? throw new Exception("ADMIN_PASSWORD not set!");

using (var scope = app.Services.CreateScope())
{
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleMgr.RoleExistsAsync("Admin"))
        await roleMgr.CreateAsync(new IdentityRole("Admin"));

    //var admin = await userMgr.FindByEmailAsync("admin@stadio.local");
    //if (admin is null)
    //{
    //    admin = new ApplicationUser { UserName = "admin@stadio.local", Email = "admin@stadio.local" };
    //    var result = await userMgr.CreateAsync(admin, adminPw);
    //    if (!result.Succeeded) throw new Exception(string.Join(';', result.Errors.Select(e => e.Description)));
    //    await userMgr.AddToRoleAsync(admin, "Admin");
    //}
}

// ——————————————————————————————
// 4.  MIDDLEWARE & ROUTING
// ——————————————————————————————
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

//builder.Services.ConfigureApplicationCookie(options => { … })
//builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", options => { … })

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
