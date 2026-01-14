using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;
using Vahicle_Rental_System.Models.ViewModels;

namespace Vahicle_Rental_System.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private bool IsAdmin()
        {
            return User.Identity.Name != null &&
                   User.Identity.Name.Equals("admin@carbook.com", StringComparison.OrdinalIgnoreCase);
        }

        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var cars = _context.Cars.Select(c => new Car
            {
                Id = c.Id,
                Brand = c.Brand,
                Model = c.Model,
                PriceSelfDrive = c.PriceSelfDrive,
                PriceWithDriver = c.PriceWithDriver,
                ImageUrl = c.ImageUrl,
                IsAvailable = c.IsAvailable,
                Category = c.Category
            }).ToList();

            return View(cars);
        }

        [HttpGet]
        public IActionResult AddCar()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

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

        [HttpPost]
        public async Task<IActionResult> AddCar(CarViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // 1. Manually read form data (Fixes validation failure)
            if (string.IsNullOrEmpty(model.Brand)) model.Brand = Request.Form["Brand"];
            if (string.IsNullOrEmpty(model.Model)) model.Model = Request.Form["Model"];
            if (string.IsNullOrEmpty(model.Description)) model.Description = Request.Form["Description"];

            if (model.PriceSelfDrive == 0 && decimal.TryParse(Request.Form["PriceSelfDrive"], out decimal self)) model.PriceSelfDrive = self;
            if (model.PriceWithDriver == 0 && decimal.TryParse(Request.Form["PriceWithDriver"], out decimal driver)) model.PriceWithDriver = driver;
            if (model.OvertimeRate == 0 && decimal.TryParse(Request.Form["OvertimeRate"], out decimal overtime)) model.OvertimeRate = overtime;
            if (model.Seats == 0 && int.TryParse(Request.Form["Seats"], out int seats)) model.Seats = seats;
            if (model.CategoryId == 0 && int.TryParse(Request.Form["CategoryId"], out int catId)) model.CategoryId = catId;

            // 2. Clear validation errors for manually fixed fields
            if (!string.IsNullOrEmpty(model.Brand) && !string.IsNullOrEmpty(model.Model))
            {
                ModelState.Remove("Brand");
                ModelState.Remove("Model");
                ModelState.Remove("PriceSelfDrive");
                ModelState.Remove("PriceWithDriver");
            }

            // 3. Handle Image
            string uniqueFileName = null;
            var files = Request.Form.Files;
            if (files.Count > 0)
            {
                var file = files[0];
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "cars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }

            // 4. Save
            if (!string.IsNullOrEmpty(model.Brand) && !string.IsNullOrEmpty(model.Model) && model.PriceSelfDrive > 0)
            {
                var car = new Car
                {
                    Brand = model.Brand,
                    Model = model.Model,
                    Description = model.Description ?? "",
                    PriceSelfDrive = model.PriceSelfDrive,
                    PriceWithDriver = model.PriceWithDriver,
                    OvertimeRate = model.OvertimeRate,
                    FuelPolicy = Request.Form["FuelPolicy"].ToString(),
                    Transmission = Request.Form["Transmission"].ToString(),
                    Seats = model.Seats,
                    CategoryId = model.CategoryId,
                    VideoUrl = Request.Form["VideoUrl"].ToString(),
                    IsAvailable = true,
                    ImageUrl = uniqueFileName != null ? "/images/cars/" + uniqueFileName : "/images/default_car.jpg"
                };

                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }

            model.Categories = _context.Categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            return View(model);
        }

        [HttpPost]
        public IActionResult DeleteCar(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var car = _context.Cars.Find(id);
            if (car != null) { _context.Cars.Remove(car); _context.SaveChanges(); }
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult EditCar(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var car = _context.Cars.Find(id);
            if (car == null) return NotFound();

            var model = new CarViewModel
            {
                Id = car.Id,
                Brand = car.Brand,
                Model = car.Model,
                Description = car.Description,
                PriceSelfDrive = car.PriceSelfDrive,
                PriceWithDriver = car.PriceWithDriver,
                OvertimeRate = car.OvertimeRate,
                FuelPolicy = car.FuelPolicy,
                Transmission = car.Transmission,
                Seats = car.Seats,
                CategoryId = car.CategoryId,
                VideoUrl = car.VideoUrl,
                ExistingImageUrl = car.ImageUrl,
                Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList()
            };

            return View(model);
        }

        // --- FIXED EDIT CAR POST ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCar(CarViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // 1. Manually read ID (CRITICAL FIX: In case binding fails for ID too)
            if (model.Id == 0 && int.TryParse(Request.Form["Id"], out int id)) model.Id = id;

            // 2. Manually read other fields
            if (string.IsNullOrEmpty(model.Brand)) model.Brand = Request.Form["Brand"];
            if (string.IsNullOrEmpty(model.Model)) model.Model = Request.Form["Model"];
            if (string.IsNullOrEmpty(model.Description)) model.Description = Request.Form["Description"];

            if (model.PriceSelfDrive == 0 && decimal.TryParse(Request.Form["PriceSelfDrive"], out decimal self)) model.PriceSelfDrive = self;
            if (model.PriceWithDriver == 0 && decimal.TryParse(Request.Form["PriceWithDriver"], out decimal driver)) model.PriceWithDriver = driver;
            if (model.OvertimeRate == 0 && decimal.TryParse(Request.Form["OvertimeRate"], out decimal overtime)) model.OvertimeRate = overtime;
            if (model.Seats == 0 && int.TryParse(Request.Form["Seats"], out int seats)) model.Seats = seats;
            if (model.CategoryId == 0 && int.TryParse(Request.Form["CategoryId"], out int catId)) model.CategoryId = catId;

            // 3. Find the car
            var car = _context.Cars.Find(model.Id);
            if (car == null) return NotFound();

            // 4. Handle Image Update
            var files = Request.Form.Files;
            if (files.Count > 0)
            {
                var file = files[0];
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "cars");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                car.ImageUrl = "/images/cars/" + uniqueFileName;
            }

            // 5. Update Fields
            car.Brand = model.Brand;
            car.Model = model.Model;
            car.Description = model.Description;
            car.PriceSelfDrive = model.PriceSelfDrive;
            car.PriceWithDriver = model.PriceWithDriver;
            car.OvertimeRate = model.OvertimeRate;
            car.FuelPolicy = Request.Form["FuelPolicy"].ToString();
            car.Transmission = Request.Form["Transmission"].ToString();
            car.Seats = model.Seats;
            car.CategoryId = model.CategoryId;
            car.VideoUrl = Request.Form["VideoUrl"].ToString();

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Car updated successfully!";
            return RedirectToAction("Dashboard", "Admin");
        }
    }
}