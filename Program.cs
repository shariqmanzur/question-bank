using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestionBank.Data;
using QuestionBank.Services;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ---------- Connection String ----------
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Server=(localdb)\\mssqllocaldb;Database=QuestionBankDb;Trusted_Connection=True;MultipleActiveResultSets=true";
}

// ---------- Services Registration ----------
// EF Core + SQL Server with split-query by default
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseSqlServer(connectionString, sqlOpts =>
            sqlOpts.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
        .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
);

// Identity with Roles, plus cookie config for “Remember Me”
builder.Services
    .AddDefaultIdentity<IdentityUser>(opts =>
    {
        opts.SignIn.RequireConfirmedAccount = false;
        // you can tweak password strength, lockout, etc. here if you like
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure the application cookie so that “Remember Me” (persistent cookies) works:
builder.Services.ConfigureApplicationCookie(options =>
{
    // If the user checks “Remember me?”, this is how long the cookie lives
    options.ExpireTimeSpan = TimeSpan.FromDays(14);

    // If you leave off “Remember me”, it'll still re-issue the cookie on each request
    options.SlidingExpiration = true;

    // Paths for login / logout / access denied (optional – these are defaults)
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Make the cookie HTTP-only for security
    options.Cookie.HttpOnly = true;
    // Name it something unique if you host multiple apps
    options.Cookie.Name = "QuestionBank.Auth";
});

// Application services
builder.Services.AddScoped<INotificationService, NotificationService>();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// for PDF
RotativaConfiguration.Setup(
    app.Environment.WebRootPath,   // <-- this is wwwroot
    "Rotativa"                     // <-- the folder name under wwwroot
);

// ---------- Seed Roles ----------
app.SeedRoles();

// ---------- Middleware Pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Detailed}/{id?}");
app.MapRazorPages();

app.Run();

// ---------- Extension for Seeding Roles ----------
static class ApplicationBuilderExtensions
{
    public static void SeedRoles(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { "Admin", "Teacher" };

        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            }
        }
    }
}