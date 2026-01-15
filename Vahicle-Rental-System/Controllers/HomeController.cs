using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;

namespace Vahicle_Rental_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        // FIXED: There is now ONLY ONE constructor. 
        // This accepts both the Logger and the DbContext.
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Home";
            // Fetch cars from database to show on the Home page
            var cars = _context.Cars.Include(c => c.Category).ToList();
            return View(cars);
        }

        public IActionResult Cars()
        {
            ViewData["ActivePage"] = "Cars";
            // Fetch all cars from database for the listing page
            var cars = _context.Cars.Include(c => c.Category).ToList();
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

        public IActionResult Pricing()
        {
            ViewData["ActivePage"] = "Pricing";
            return View();
        }

        public IActionResult Blog()
        {
            ViewData["ActivePage"] = "Blog";
            return View();
        }

        public IActionResult BlogDetails()
        {
            ViewData["ActivePage"] = "Blog";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["ActivePage"] = "Contact";
            return View();
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