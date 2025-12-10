using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;

namespace Vahicle_Rental_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // --- SOCIAL LOGIN START ---

        // 1. Redirects user to Google/Facebook/Twitter
        public IActionResult SocialLogin(string provider)
        {
            var redirectUrl = Url.Action("SocialResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        // 2. Handles the callback when they return
        public async Task<IActionResult> SocialResponse()
        {
            // Get user info from the provider
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result?.Principal == null)
            {
                return RedirectToAction("Login");
            }

            // Extract claims
            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
            {
                // Error: Provider didn't return an email
                return RedirectToAction("Login");
            }

            // Check if user exists in DB
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                // Create new user if they don't exist
                user = new User
                {
                    FullName = name ?? "User",
                    Email = email,
                    // Assign random password since they use social login
                    Password = HashPassword(Guid.NewGuid().ToString())
                };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            // --- CRITICAL STEP: Re-issue Cookie with App Claims ---
            // We replace the "Google" identity with our "App" identity so UserId is available
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", user.FullName ?? "User"),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()) // Store DB ID
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Sign In (Overwrites the temporary social cookie)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true }
            );

            return RedirectToAction("Index", "Home");
        }

        // --- SOCIAL LOGIN END ---

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            ViewData["IsRegister"] = true;
            return View("Auth");
        }

        // POST: Register
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already taken.");
                    ViewData["IsRegister"] = true;
                    return View("Auth");
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = HashPassword(model.Password)
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }

            ViewData["IsRegister"] = true;
            return View("Auth", new { RegisterModel = model });
        }

        // GET: Login
        [HttpGet]
        public IActionResult Login()
        {
            ViewData["IsRegister"] = false;
            return View("Auth");
        }

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // --- ADMIN SEEDING LOGIC ---
            // This ensures the Admin account exists the first time you try to log in.
            if (!string.IsNullOrEmpty(model.Email) && model.Email.ToLower() == "admin@carbook.com")
            {
                var adminUser = _context.Users.FirstOrDefault(u => u.Email == "admin@carbook.com");
                if (adminUser == null)
                {
                    var newAdmin = new User
                    {
                        Email = "admin@carbook.com",
                        FullName = "System Administrator",
                        // Hardcoded password: admin123
                        Password = HashPassword("admin123")
                    };
                    _context.Users.Add(newAdmin);
                    _context.SaveChanges();
                }
            }
            // ---------------------------

            if (ModelState.IsValid)
            {
                var hashedPassword = HashPassword(model.Password);
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == hashedPassword);

                if (user != null)
                {
                    // Use helper method to sign in with consistent claims
                    await SignInUser(user);
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            ViewData["IsRegister"] = false;
            return View("Auth");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // GET: Edit Profile
        [HttpGet]
        public IActionResult EditProfile()
        {
            var userEmail = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            if (user == null) return RedirectToAction("Login");

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email
            };

            return View(model);
        }

        // POST: Edit Profile
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var currentEmail = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == currentEmail);

            if (user == null) return RedirectToAction("Login");

            // 1. Update Basic Info
            user.FullName = model.FullName;

            // 2. Update Email (Only if changed)
            if (user.Email != model.Email)
            {
                // Check if new email is taken
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already in use.");
                    return View(model);
                }
                user.Email = model.Email;
            }

            // 3. Update Password (Only if provided)
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                user.Password = HashPassword(model.NewPassword);
            }

            _context.SaveChanges();

            // Refresh the user's cookie to show new Name/Email immediately
            await SignInUser(user);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index", "Home");
        }

        // Helper method to refresh cookie (Reuse the logic from Login)
        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", user.FullName ?? "User"),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties());
        }
    }
}