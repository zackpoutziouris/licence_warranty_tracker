using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebProject.Data;
using Microsoft.AspNetCore.Identity;
using WebProject.Models;

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

// Register cookie authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Protect Dashboard, but allow users to access Login without signing in
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Dashboard");
    options.Conventions.AllowAnonymousToPage("/Login");
});

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