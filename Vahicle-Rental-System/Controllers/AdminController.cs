using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Fixes 'SelectListItem' error
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;
using Vahicle_Rental_System.Models.ViewModels;

namespace Vahicle_Rental_System.Controllers
{
    // 1. Security: Only Admin can access
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Needed for image uploads

        // 2. Constructor: Injects Database and Environment
        public AdminController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- HELPER: Checks if user is admin ---
        private bool IsAdmin()
        {
            return User.Identity.Name != null &&
                   User.Identity.Name.Equals("admin@carbook.com", StringComparison.OrdinalIgnoreCase);
        }

        // --- DASHBOARD: List all cars ---
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Include the Category data so we can show "SUV", "Luxury" etc. in the list
            var cars = _context.Cars
                .Select(c => new Car
                {
                    Id = c.Id,
                    Brand = c.Brand,
                    Model = c.Model,
                    PriceSelfDrive = c.PriceSelfDrive,
                    PriceWithDriver = c.PriceWithDriver,
                    ImageUrl = c.ImageUrl,
                    VideoUrl = c.VideoUrl,
                    IsAvailable = c.IsAvailable,
                    Category = c.Category
                })
                .ToList();

            return View(cars);
        }

        // --- ADD CAR (GET): Show the form ---
        [HttpGet]
        public IActionResult AddCar()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // 1. Create Default Categories if they don't exist yet
            if (!_context.Categories.Any())
            {
                _context.Categories.AddRange(
                    new Category { Name = "Standard" },
                    new Category { Name = "SUV" },
                    new Category { Name = "Luxury" },
                    new Category { Name = "Sports" }
                );
                _context.SaveChanges();
            }

            // 2. Prepare the ViewModel with the Dropdown List
            var model = new CarViewModel
            {
                Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList()
            };

            return View(model);
        }

        // --- ADD CAR (POST): Process the form ---
        [HttpPost]
        public async Task<IActionResult> AddCar(CarViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                string uniqueFileName = null;

                // 3. IMAGE UPLOAD LOGIC
                if (model.CarImage != null)
                {
                    // Folder: wwwroot/images/cars
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "cars");

                    // Create folder if missing
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    // Unique Name
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.CarImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save File
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CarImage.CopyToAsync(fileStream);
                    }
                }

                // 4. SAVE TO DATABASE
                var car = new Car
                {
                    Brand = model.Brand,
                    Model = model.Model,
                    Description = model.Description,

                    // Pricing Rules
                    PriceSelfDrive = model.PriceSelfDrive,
                    PriceWithDriver = model.PriceWithDriver,
                    OvertimeRate = model.OvertimeRate,
                    FuelPolicy = model.FuelPolicy ?? "Refill or pay per KM",

                    // Specs
                    Transmission = model.Transmission,
                    Seats = model.Seats,

                    // Relationships & Media
                    CategoryId = model.CategoryId,
                    VideoUrl = model.VideoUrl,
                    IsAvailable = true,
                    ImageUrl = uniqueFileName != null ? "/images/cars/" + uniqueFileName : "/images/default_car.jpg"
                };

                _context.Cars.Add(car);
                await _context.SaveChangesAsync();

                return RedirectToAction("Dashboard");
            }

            // If validation fails, reload the dropdown so the page doesn't break
            model.Categories = _context.Categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            return View(model);
        }

        // --- DELETE CAR ---
        [HttpPost]
        public IActionResult DeleteCar(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var car = _context.Cars.Find(id);
            if (car != null)
            {
                _context.Cars.Remove(car);
                _context.SaveChanges();
            }
            return RedirectToAction("Dashboard");
        }
    }
}