using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // Required for .Include()
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
                Category = c.Category,
                Mileage = c.Mileage,    // Load new data
                FuelType = c.FuelType   // Load new data
            }).ToList();

            return View(cars);
        }

        /// --- MANAGE BOOKINGS (With Search & Filter) ---
        public IActionResult ManageBookings(string searchString, string category, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // 1. Start with all bookings including related data
            var bookingsQuery = _context.Bookings
                .Include(b => b.Car)
                .ThenInclude(c => c.Category) // Include Category for filtering
                .AsQueryable();

            // 2. Search Logic (Matches Booker Name, Phone, or Car Name)
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                bookingsQuery = bookingsQuery.Where(b =>
                    b.BookerName.ToLower().Contains(searchString) ||
                    b.BookerPhone.Contains(searchString) ||
                    b.Car.Brand.ToLower().Contains(searchString) ||
                    b.Car.Model.ToLower().Contains(searchString));
            }

            // 3. Filter by Category
            if (!string.IsNullOrEmpty(category))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Car.Category.Name == category);
            }

            // 4. Filter by Status (Optional, good for "Show only Active")
            if (!string.IsNullOrEmpty(status))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status == status);
            }

            // Load data for the Dropdown Filter
            ViewBag.Categories = _context.Categories.Select(c => c.Name).Distinct().ToList();

            // Execute Query
            var bookings = bookingsQuery.OrderByDescending(b => b.CreatedAt).ToList();

            return View(bookings);
        }

        // --- NEW ACTION: RETURN CAR ---
        [HttpPost]
        public async Task<IActionResult> ReturnCar(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var booking = await _context.Bookings.Include(b => b.Car).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            if (booking.Status == "Confirmed")
            {
                // 1. Mark Booking as Returned
                booking.Status = "Returned";

                // 2. Make Car Available Again
                if (booking.Car != null)
                {
                    booking.Car.IsAvailable = true;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Car marked as returned successfully!";
            }

            return RedirectToAction("ManageBookings");
        }

        [HttpGet]
        public IActionResult AddCar()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (!_context.Categories.Any())
            {
                _context.Categories.AddRange(
                    new Category { Name = "Standard" }, new Category { Name = "SUV" },
                    new Category { Name = "Luxury" }, new Category { Name = "Sports" }
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

            // Manually bind text fields
            if (string.IsNullOrEmpty(model.Brand)) model.Brand = Request.Form["Brand"];
            if (string.IsNullOrEmpty(model.Model)) model.Model = Request.Form["Model"];
            if (string.IsNullOrEmpty(model.Description)) model.Description = Request.Form["Description"];

            // Bind new fields
            if (string.IsNullOrEmpty(model.Mileage)) model.Mileage = Request.Form["Mileage"];
            if (string.IsNullOrEmpty(model.Luggage)) model.Luggage = Request.Form["Luggage"];
            if (string.IsNullOrEmpty(model.FuelType)) model.FuelType = Request.Form["FuelType"];

            // Manually bind numbers
            if (model.PriceSelfDrive == 0 && int.TryParse(Request.Form["PriceSelfDrive"], out int self)) model.PriceSelfDrive = self;
            if (model.PriceWithDriver == 0 && int.TryParse(Request.Form["PriceWithDriver"], out int driver)) model.PriceWithDriver = driver;
            if (model.OvertimeRate == 0 && int.TryParse(Request.Form["OvertimeRate"], out int overtime)) model.OvertimeRate = overtime;
            if (model.Seats == 0 && int.TryParse(Request.Form["Seats"], out int seats)) model.Seats = seats;
            if (model.CategoryId == 0 && int.TryParse(Request.Form["CategoryId"], out int catId)) model.CategoryId = catId;

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

                // Save New Features
                Mileage = model.Mileage,
                Luggage = model.Luggage,
                FuelType = model.FuelType,

                IsAvailable = true,
                ImageUrl = uniqueFileName != null ? "/images/cars/" + uniqueFileName : "/images/default_car.jpg"
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            return RedirectToAction("Dashboard");
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

                // Map new fields to View Model
                Mileage = car.Mileage,
                Luggage = car.Luggage,
                FuelType = car.FuelType,

                ExistingImageUrl = car.ImageUrl,
                Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCar(CarViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (model.Id == 0 && int.TryParse(Request.Form["Id"], out int id)) model.Id = id;
            if (string.IsNullOrEmpty(model.Brand)) model.Brand = Request.Form["Brand"];
            if (string.IsNullOrEmpty(model.Model)) model.Model = Request.Form["Model"];
            if (string.IsNullOrEmpty(model.Description)) model.Description = Request.Form["Description"];

            // Manual read for new fields
            if (string.IsNullOrEmpty(model.Mileage)) model.Mileage = Request.Form["Mileage"];
            if (string.IsNullOrEmpty(model.Luggage)) model.Luggage = Request.Form["Luggage"];
            if (string.IsNullOrEmpty(model.FuelType)) model.FuelType = Request.Form["FuelType"];

            if (model.PriceSelfDrive == 0 && int.TryParse(Request.Form["PriceSelfDrive"], out int self)) model.PriceSelfDrive = self;
            if (model.PriceWithDriver == 0 && int.TryParse(Request.Form["PriceWithDriver"], out int driver)) model.PriceWithDriver = driver;
            if (model.OvertimeRate == 0 && int.TryParse(Request.Form["OvertimeRate"], out int overtime)) model.OvertimeRate = overtime;
            if (model.Seats == 0 && int.TryParse(Request.Form["Seats"], out int seats)) model.Seats = seats;
            if (model.CategoryId == 0 && int.TryParse(Request.Form["CategoryId"], out int catId)) model.CategoryId = catId;

            var car = _context.Cars.Find(model.Id);
            if (car == null) return NotFound();

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

            // Update DB with new fields
            car.Mileage = model.Mileage;
            car.Luggage = model.Luggage;
            car.FuelType = model.FuelType;

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Car updated successfully!";
            return RedirectToAction("Dashboard", "Admin");
        }
    }
}