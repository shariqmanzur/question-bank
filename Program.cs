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

// Identity with Roles
builder.Services
    .AddDefaultIdentity<IdentityUser>(opts => opts.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

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