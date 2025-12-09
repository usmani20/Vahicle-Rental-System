using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication.Facebook; // Add this using directive
//using Microsoft.AspNetCore.Authentication.Twitter; // Add this using directive
using Microsoft.EntityFrameworkCore;
using Vahicle_Rental_System.Data;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- UPDATED AUTHENTICATION CONFIGURATION ---
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
// 1. Google Login
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = "546582215782-7fphlatbreiebkmp1l2g2doptbgbgul3.apps.googleusercontent.com";
    googleOptions.ClientSecret = "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";
})
// 2. Facebook Login
//.AddFacebook(facebookOptions =>
//{
//    facebookOptions.AppId = "YOUR_FACEBOOK_APP_ID";
//    facebookOptions.AppSecret = "YOUR_FACEBOOK_APP_SECRET";
//})
// 3. Twitter Login
//.AddTwitter(twitterOptions =>
//{
//    twitterOptions.ConsumerKey = "YOUR_TWITTER_API_KEY";
//    twitterOptions.ConsumerSecret = "YOUR_TWITTER_API_SECRET";
//});
// --------------------------------------------

builder.Services.AddControllersWithViews();

var app = builder.Build();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();