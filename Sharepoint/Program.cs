using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sharepoint.Data;
using Sharepoint.Models;
using Sharepoint.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// ── Single seed block ────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any(u => u.Username == "admin"))
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = Convert.ToBase64String(
            sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("Admin@1234")));

        db.Users.Add(new AppUser
        {
            Username = "admin",
            DisplayName = "Administrator",
            PasswordHash = hash,
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}
// ────────────────────────────────────────────────────────────────

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=WorkInstruction}/{id?}");

app.Run();