using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Vahicle_Rental_System.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userEmail = User.Identity?.Name;

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UserEmail == userEmail);

            if (booking == null) return NotFound();

            if (booking.Status == "Pending")
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Your pending reservation has been successfully removed.";
            }
            else
            {
                TempData["Error"] = "Confirmed bookings cannot be cancelled via dashboard.";
            }

            return RedirectToAction("MyBookings");
        }

        // --- FIX IS HERE: Added BookerName, BookerPhone, BookerAddress parameters ---
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Checkout(int CarId, DateTime PickupDate, DateTime ReturnDate,
                                         string RentalType, string PickupLocation, string DropoffLocation,
                                         string BookerName, string BookerPhone, string BookerAddress) // <--- Added params
        {
            var userEmail = User.Identity?.Name;

            // 1. Prevent double booking
            bool alreadyBookedByUser = await _context.Bookings.AnyAsync(b =>
                b.CarId == CarId &&
                b.UserEmail == userEmail &&
                (b.Status == "Confirmed" || b.Status == "Pending"));

            if (alreadyBookedByUser)
            {
                TempData["Error"] = "You already have a pending or confirmed booking for this car.";
                return RedirectToAction("CarDetails", "Home", new { id = CarId });
            }

            // 2. Availability Check
            bool isTaken = await _context.Bookings.AnyAsync(b =>
                b.CarId == CarId && b.Status == "Confirmed" &&
                ((PickupDate < b.ReturnDate) && (ReturnDate > b.PickupDate)));

            if (isTaken)
            {
                TempData["Error"] = "This vehicle is reserved by another customer for these dates.";
                return RedirectToAction("CarDetails", "Home", new { id = CarId });
            }

            var car = await _context.Cars.FindAsync(CarId);
            if (car == null) return NotFound();

            // 3. Price Calculation
            int days = (int)(ReturnDate - PickupDate).TotalDays;
            if (days <= 0) days = 1;
            int rate = (RentalType == "SelfDrive") ? car.PriceSelfDrive : car.PriceWithDriver;

            // 4. Create Booking (NOW SAVING CONTACT INFO)
            var booking = new Booking
            {
                CarId = CarId,
                UserEmail = userEmail ?? "guest@customer.com",

                // --- SAVE THE NEW DATA ---
                BookerName = BookerName,
                BookerPhone = BookerPhone,
                BookerAddress = BookerAddress,
                // ------------------------------------

                PickupDate = PickupDate,
                ReturnDate = ReturnDate,
                PickupLocation = PickupLocation,
                DropoffLocation = DropoffLocation,
                RentalType = RentalType,
                TotalPrice = rate * days,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(); // This will now succeed because BookerAddress is not null

            return RedirectToAction("Payment", new { id = booking.Id });
        }

        public async Task<IActionResult> Payment(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Car).FirstOrDefaultAsync(x => x.Id == id);
            if (booking == null) return NotFound();

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int BookingId, string CardNumber, string Expiry, string CVV)
        {
            var booking = await _context.Bookings.Include(b => b.Car).FirstOrDefaultAsync(x => x.Id == BookingId);
            if (booking == null) return NotFound();

            string cleanCardNumber = CardNumber?.Replace(" ", "") ?? "";

            if (string.IsNullOrEmpty(cleanCardNumber) || cleanCardNumber.Length != 16)
            {
                ViewBag.Error = "Invalid Card Details.";
                return View("Payment", booking);
            }

            booking.Status = "Confirmed";
            booking.TransactionId = "PKR-TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // Mark car unavailable
            var car = await _context.Cars.FindAsync(booking.CarId);
            if (car != null) car.IsAvailable = false;

            await _context.SaveChangesAsync();
            SendEmail(booking);

            return RedirectToAction("Receipt", new { id = booking.Id });
        }

        public async Task<IActionResult> Receipt(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Car).FirstOrDefaultAsync(x => x.Id == id);
            if (booking == null || booking.Status != "Confirmed") return RedirectToAction("Index", "Home");

            return View(booking);
        }

        public async Task<IActionResult> MyBookings()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Where(b => b.UserEmail == userEmail)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(bookings);
        }

        private void SendEmail(Booking b)
        {
            try
            {
                var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("carbooooooooook@gmail.com", "nchxasxpbbwvmljx"),
                    EnableSsl = true,
                };

                var msg = new MailMessage("carbooooooooook@gmail.com", b.UserEmail)
                {
                    Subject = "Booking Confirmed - Car Rental System",
                    Body = $@"
                        <div style='font-family: sans-serif; border: 1px solid #eee; padding: 20px;'>
                            <h2 style='color: #01d28e;'>Rental Confirmed!</h2>
                            <p><b>Vehicle:</b> {b.Car.Brand} {b.Car.Model}</p>
                            <p><b>Booker:</b> {b.BookerName} ({b.BookerPhone})</p>
                            <p><b>Pick-up:</b> {b.PickupLocation}</p>
                            <p><b>Dates:</b> {b.PickupDate:d} to {b.ReturnDate:d}</p>
                            <hr/>
                            <h3>Total Paid: PKR {b.TotalPrice:N0}</h3>
                        </div>",
                    IsBodyHtml = true
                };
                smtp.Send(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email Error: " + ex.Message);
            }
        }
    }
}