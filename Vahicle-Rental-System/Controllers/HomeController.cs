using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;
using System.Net;
using System.Net.Mail;

namespace Vahicle_Rental_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Home";
            // Fetches top 6 cars for the Featured Vehicles carousel
            var cars = _context.Cars
                .Include(c => c.Category)
                .OrderByDescending(c => c.PriceSelfDrive)
                .Take(6)
                .ToList();
            return View(cars);
        }

        public async Task<IActionResult> Cars(string category, string searchString)
        {
            ViewData["ActivePage"] = "Cars";
            ViewBag.Categories = await _context.Categories
                                               .Select(c => c.Name)
                                               .Distinct()
                                               .ToListAsync();

            var carsQuery = _context.Cars.Include(c => c.Category).AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                carsQuery = carsQuery.Where(c => c.Category.Name == category);
                ViewBag.CurrentCategory = category;
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();
                carsQuery = carsQuery.Where(s =>
                    (s.Brand + " " + s.Model).ToLower().Contains(searchString) ||
                    s.Brand.ToLower().Contains(searchString) ||
                    s.Model.ToLower().Contains(searchString));

                ViewBag.CurrentSearch = searchString;
            }

            var cars = await carsQuery.ToListAsync();
            return View(cars);
        }

        public IActionResult CarDetails(int id)
        {
            ViewData["ActivePage"] = "Cars";
            var car = _context.Cars.Include(c => c.Category).FirstOrDefault(c => c.Id == id);
            if (car == null) return NotFound();
            return View(car);
        }

        public IActionResult About()
        {
            ViewData["ActivePage"] = "About";
            return View();
        }

        public IActionResult Services()
        {
            ViewData["ActivePage"] = "Services";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["ActivePage"] = "Contact";
            return View();
        }

        // POST: Contact - Handles the contact form submission
        [HttpPost]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress(email);
                mail.To.Add("carbooooooooook@gmail.com");
                mail.Subject = $"Contact Form: {subject}";
                mail.Body = $"Name: {name}\nEmail: {email}\n\nMessage:\n{message}";
                mail.IsBodyHtml = false;

                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("carbooooooooook@gmail.com", "nchxasxpbbwvmljx");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }

                TempData["Success"] = "Thank you for contacting Carbook! We will get back to you soon.";
            }
            catch (Exception)
            {
                ViewBag.Error = "Sorry, there was an error sending your message.";
            }

            return RedirectToAction("Contact");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}