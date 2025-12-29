using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
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

        // --- AJAX: SEND OTP ACTION ---
        [HttpPost]
        [HttpPost]
        public IActionResult SendOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Please enter an email address." });

            // Ensure Azure Firewall allows your IP to avoid SqlException
            if (_context.Users.Any(u => u.Email == email))
                return Json(new { success = false, message = "This email is already registered." });

            var otp = new Random().Next(100000, 999999).ToString();

            // Store in Session (Fixes 'Session not configured' error)
            HttpContext.Session.SetString("CurrentOtp", otp);
            HttpContext.Session.SetString("CurrentEmail", email);

            try
            {
                SendEmailHelper(email, "CarBook Verification Code", otp);
                return Json(new { success = true, message = "OTP sent to your email!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Email failed: " + ex.Message });
            }
        }

        // --- POST: REGISTER ---
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Retrieve OTP from Session
                var sessionOtp = HttpContext.Session.GetString("CurrentOtp");
                var sessionEmail = HttpContext.Session.GetString("CurrentEmail");

                // 2. Validate OTP Match
                if (sessionOtp == null || model.Otp != sessionOtp)
                {
                    ModelState.AddModelError("Otp", "Invalid or expired OTP. Please send code again.");
                    ViewData["IsRegister"] = true;
                    return View("Auth", model);
                }

                // 3. Verify Email consistency
                if (model.Email != sessionEmail)
                {
                    ModelState.AddModelError("Email", "Email does not match the verified address.");
                    ViewData["IsRegister"] = true;
                    return View("Auth", model);
                }

                // 4. Final DB Check
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already taken.");
                    ViewData["IsRegister"] = true;
                    return View("Auth", model);
                }

                // 5. Create User
                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = HashPassword(model.Password),
                    IsEmailVerified = true // Verified via the OTP process
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // Clear Session after successful registration
                HttpContext.Session.Remove("CurrentOtp");
                HttpContext.Session.Remove("CurrentEmail");

                return RedirectToAction("Login");
            }

            ViewData["IsRegister"] = true;
            return View("Auth", model);
        }

        // --- GET: REGISTER & LOGIN ---
        [HttpGet]
        public IActionResult Register() { ViewData["IsRegister"] = true; return View("Auth"); }

        [HttpGet]
        public IActionResult Login() { ViewData["IsRegister"] = false; return View("Auth"); }

        // --- POST: LOGIN ---
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Admin Seeding
            if (!string.IsNullOrEmpty(model.Email) && model.Email.ToLower() == "admin@carbook.com")
            {
                if (!_context.Users.Any(u => u.Email == "admin@carbook.com"))
                {
                    _context.Users.Add(new User { Email = "admin@carbook.com", FullName = "System Admin", Password = HashPassword("admin123"), IsEmailVerified = true });
                    _context.SaveChanges();
                }
            }

            if (ModelState.IsValid)
            {
                var hashedPassword = HashPassword(model.Password);
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == hashedPassword);

                if (user != null)
                {
                    await SignInUser(user);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            ViewData["IsRegister"] = false;
            return View("Auth");
        }

        // --- SOCIAL LOGIN ---
        public IActionResult SocialLogin(string provider)
        {
            var redirectUrl = Url.Action("SocialResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> SocialResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result?.Principal == null) return RedirectToAction("Login");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                user = new User { FullName = name ?? "User", Email = email, Password = HashPassword(Guid.NewGuid().ToString()), IsEmailVerified = true };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            await SignInUser(user);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // --- EDIT PROFILE ---
        [HttpGet]
        public IActionResult EditProfile()
        {
            var userEmail = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login");
            return View(new EditProfileViewModel { FullName = user.FullName, Email = user.Email });
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = _context.Users.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (user == null) return RedirectToAction("Login");

            user.FullName = model.FullName;
            if (user.Email != model.Email && !_context.Users.Any(u => u.Email == model.Email)) user.Email = model.Email;
            if (!string.IsNullOrEmpty(model.NewPassword)) user.Password = HashPassword(model.NewPassword);

            _context.SaveChanges();
            await SignInUser(user);
            return RedirectToAction("Index", "Home");
        }

        // --- PRIVATE HELPERS ---
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", user.FullName ?? "User"),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
        }

        private void SendEmailHelper(string toEmail, string subject, string otpCode)
        {
            var fromAddress = new MailAddress("carbooooooooook@gmail.com", "CarBook Rental");
            var toAddress = new MailAddress(toEmail);

            // Removing spaces from the 16-character App Password
            const string fromPassword = "nchx asxp bbwv mljx";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                // The .Replace(" ", "") ensures the 16 characters are joined correctly
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword.Replace(" ", ""))
            };

            string htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #eee; padding: 20px;'>
                <h2 style='color: #01d28e;'>CarBook Verification Code</h2>
                <p>Your OTP code is: <b style='font-size: 24px;'>{otpCode}</b></p>
                <p>This code will expire in 20 minutes.</p>
            </div>";

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }


        // --- FORGOT PASSWORD: SEND OTP ---
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Please enter your email." });

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return Json(new { success = false, message = "Email not found." });

            var otp = new Random().Next(100000, 999999).ToString();

            // Store Reset Info in Session
            HttpContext.Session.SetString("ResetOtp", otp);
            HttpContext.Session.SetString("ResetEmail", email);

            try
            {
                SendEmailHelper(email, "CarBook Password Reset Code", otp);
                return Json(new { success = true, message = "Reset code sent to your email!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // --- FORGOT PASSWORD: RESET ACTION ---
        [HttpPost]
        public IActionResult ResetPassword(string email, string otp, string newPassword)
        {
            var sessionOtp = HttpContext.Session.GetString("ResetOtp");
            var sessionEmail = HttpContext.Session.GetString("ResetEmail");

            if (sessionOtp == null || otp != sessionOtp || email != sessionEmail)
            {
                return Json(new { success = false, message = "Invalid or expired reset code." });
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                user.Password = HashPassword(newPassword);
                _context.SaveChanges();

                // Clear session after successful reset
                HttpContext.Session.Remove("ResetOtp");
                HttpContext.Session.Remove("ResetEmail");

                return Json(new { success = true, message = "Password reset successfully! Please login." });
            }

            return Json(new { success = false, message = "User not found." });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }



    }
}