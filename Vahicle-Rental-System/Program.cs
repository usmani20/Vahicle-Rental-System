using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount; // Ensure you installed this package
using Microsoft.EntityFrameworkCore;
using Vahicle_Rental_System.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Authentication Configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
})
// --- SOCIAL LOGINS ---
// A. Google Login
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Athentication:GoogleClientID"];
    googleOptions.ClientSecret = builder.Configuration["Athentication:GoogleClientSecret"];
})
// B. Facebook Login
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["Athentication:FacebookAppId"];
    facebookOptions.AppSecret = builder.Configuration["Athentication:FacebookAppSecret"];
})
// C. Microsoft (Outlook) Login
.AddMicrosoftAccount(microsoftOptions =>
{
    microsoftOptions.ClientId = builder.Configuration["Athentication:MicrosoftClientId"];
    microsoftOptions.ClientSecret = builder.Configuration["Athentication:MicrosoftClientSecret"];
});
// ---------------------

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Enable Auth Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();