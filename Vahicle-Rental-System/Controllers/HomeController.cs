using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Vahicle_Rental_System.Models;

namespace Vahicle_Rental_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Home";
            return View();
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

        public IActionResult Cars()
        {
            ViewData["ActivePage"] = "Cars";
            return View();
        }

        // Handles the "Details" button for cars
        public IActionResult CarDetails()
        {
            // Keep "Cars" highlighted in the menu
            ViewData["ActivePage"] = "Cars";
            return View();
        }

        public IActionResult Blog()
        {
            ViewData["ActivePage"] = "Blog";
            return View();
        }

        // Handles the "Read More" button for blogs
        public IActionResult BlogDetails()
        {
            // Keep "Blog" highlighted in the menu
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