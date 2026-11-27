using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebProject.Data;
using WebProject.Models;
using WebProject.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Get database connection string
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// Connect to SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IPasswordHasher<Staff>, PasswordHasher<Staff>>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHostedService<NotificationBackgroundService>();
builder.Services.AddScoped<AuditPdfService>();

// Register cookie authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";

        // Authentication cookie lifetime
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Cookie security
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Validate the session against the database
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;

            string? staffId = principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            string? sessionVersion = principal?
                .FindFirstValue("SessionVersion");

            if (!int.TryParse(staffId, out int id) ||
                !int.TryParse(sessionVersion, out int version))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            // Retrieve the current staff account
            var db = context.HttpContext.RequestServices
                .GetRequiredService<ApplicationDbContext>();

            var staff = await db.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StaffId == id);

            // Reject deleted accounts and outdated sessions
            if (staff == null || staff.SessionVersion != version)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            // Ensure the cookie's role matches the database
            string expectedRole = staff.IsAdmin ? "Admin" : "Staff";

            if (!principal!.IsInRole(expectedRole))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });


builder.Services.AddAuthorization();

// Protect Dashboard, but allow users to access Login without signing in
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Dashboard");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/PasswordChanged");
});

QuestPDF.Settings.License = LicenseType.Community;
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/", () => Results.Redirect("/Login"));
app.MapRazorPages().WithStaticAssets();

app.Run();