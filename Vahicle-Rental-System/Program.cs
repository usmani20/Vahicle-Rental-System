using Microsoft.AspNetCore.Authentication.Cookies;
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
// --- FIX: Fallbacks prevent the "ClientId" startup crash ---
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:GoogleClientID"] ?? "placeholder-id";
    googleOptions.ClientSecret = builder.Configuration["Authentication:GoogleClientSecret"] ?? "placeholder-secret";
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["Authentication:FacebookAppId"] ?? "placeholder-id";
    facebookOptions.AppSecret = builder.Configuration["Authentication:FacebookAppSecret"] ?? "placeholder-secret";
})
.AddMicrosoftAccount(microsoftOptions =>
{
    microsoftOptions.ClientId = builder.Configuration["Authentication:MicrosoftClientId"] ?? "placeholder-id";
    microsoftOptions.ClientSecret = builder.Configuration["Authentication:MicrosoftClientSecret"] ?? "placeholder-secret";
});

// --- 3. FIX: REGISTER SESSION SERVICES ---
// These lines solve the "Session has not been configured" error
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- 4. FIX: MIDDLEWARE ORDER ---
// Order is critical for Authentication and Sessions to work together
app.UseAuthentication();
app.UseAuthorization();

// CRITICAL: UseSession MUST be placed after Auth and before MapControllerRoute
app.UseSession();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();